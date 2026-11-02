using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Catalog.Ports;
using LMS.Booking.Application.Console.Options;
using LMS.Booking.Application.Directory;
using LMS.Booking.Application.Guests.Ports;
using LMS.Booking.Application.Quotas.Ports;
using LMS.Booking.Application.Trips.Ports;
using LMS.Booking.Domain.Guests;
using LMS.Booking.Domain.Quotas;
using LMS.Booking.Domain.Trips;
using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Wire;
using Microsoft.Extensions.Options;

namespace LMS.Booking.Application.Trips.Services;

// 建单前置校验链（顺序即错误优先级，实施基线 D1/D2/D5）：
// 宾客存在 → 地点存在且启用 → 客户黑名单（422）→ 资格（等级门槛/游玩核验/车型权益，422）→ 容量与时间窗（40001）→ 物业配额（AUTO_REJECT 422 / WAITLIST 受理为 PENDING）。
// 任一外部源不可用一律 SourceUnavailable（503），不用默认值或空结果代替。
internal sealed class TripEligibilityService(
    IGuestDirectory guestDirectory,
    IPlatformCatalog catalog,
    IPlatformDirectory platform,
    ILoyaltyGateway loyalty,
    IPropertyQuotaRepository quotas,
    ITripRepository trips,
    IOptions<ConsoleOptions> consoleOptions,
    TimeProvider timeProvider)
{
    // 车型容量与物业配额判定与客户自助预订共用同一份实现，这里按本服务的依赖就地构造，保持校验链的构造签名不变。
    private readonly VehicleCapacityResolver capacityResolver = new(consoleOptions);
    private readonly PropertyQuotaGate quotaGate = new(quotas, trips);

    public async Task<EligibilityVerdict> EvaluateAsync(TripEvaluationRequest request, CancellationToken cancellationToken)
    {
        var guest = await guestDirectory.FindAsync(request.GuestId, cancellationToken);
        if (guest is null)
        {
            return new EligibilityVerdict(OperationResult<TripEligibility>.Failed(OperationOutcome.NotFound), RequiresManagerReview: false, Guest: null);
        }

        var pickupResult = await ReadLocationAsync(request.PickupLocationId, cancellationToken);
        if (!pickupResult.IsSuccess)
        {
            return Verdict(FailureOf(pickupResult), guest);
        }

        var dropoffResult = await ReadLocationAsync(request.DropoffLocationId, cancellationToken);
        if (!dropoffResult.IsSuccess)
        {
            return Verdict(FailureOf(dropoffResult), guest);
        }

        if (pickupResult.Value is not { IsActive: true })
        {
            return Verdict(Invalid([new FieldIssue("route.pickupLocationId", "Location is not active")]), guest);
        }

        if (dropoffResult.Value is not { IsActive: true })
        {
            return Verdict(Invalid([new FieldIssue("route.dropoffLocationId", "Location is not active")]), guest);
        }

        var restrictionFailure = await CheckRestrictionsAsync(guest, cancellationToken);
        if (restrictionFailure is not null)
        {
            return Verdict(restrictionFailure, guest);
        }

        var qualificationFailure = await CheckQualificationAsync(guest, request, cancellationToken);
        if (qualificationFailure is not null)
        {
            // 资格不满足：422 并提示走经理复核（D1）；由调用方登记待复核的资格档案。
            return new EligibilityVerdict(qualificationFailure, RequiresManagerReview: true, Guest: guest);
        }

        var capacity = capacityResolver.CapacityOf(request.VehicleClass);
        var ruleIssues = TripRules.Validate(
            request.VehicleClass,
            request.Passengers,
            request.Luggage,
            request.PickupAt,
            request.PickupMode != PickupMode.Asap,
            timeProvider.GetUtcNow(),
            consoleOptions.Value.MinimumLeadTimeMinutes,
            capacity);
        if (ruleIssues.Count > 0)
        {
            return Verdict(Invalid(ruleIssues), guest);
        }

        return await CheckQuotaAsync(request, pickupResult.Value!, dropoffResult.Value!, capacity, guest, cancellationToken);
    }

    // 客户黑名单：存在生效限制即硬拒绝（D2），例外流程后续另行设计。
    private async Task<OperationResult<TripEligibility>?> CheckRestrictionsAsync(GuestProfile guest, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(guest.MemberId))
        {
            return null;
        }

        var restrictions = await platform.CheckCustomersAsync([guest.MemberId], cancellationToken);
        if (!restrictions.IsSuccess)
        {
            return restrictions.Outcome == DirectoryOutcome.Forbidden
                ? OperationResult<TripEligibility>.Failed(OperationOutcome.Forbidden)
                : OperationResult<TripEligibility>.Failed(OperationOutcome.SourceUnavailable);
        }

        return restrictions.Value!.Count > 0
            ? Rejected([new FieldIssue("guest", "Guest has an active customer restriction")])
            : null;
    }

    // 资格：等级可用车型（含所选车型）、游玩核验。积分余额不作为拒绝条件。
    private async Task<OperationResult<TripEligibility>?> CheckQualificationAsync(
        GuestProfile guest,
        TripEvaluationRequest request,
        CancellationToken cancellationToken)
    {
        var qualification = await loyalty.GetQualificationAsync(guest.GuestId, cancellationToken);
        if (!qualification.IsSuccess)
        {
            return qualification.Outcome switch
            {
                DirectoryOutcome.NotFound => OperationResult<TripEligibility>.Failed(OperationOutcome.NotFound),
                DirectoryOutcome.Forbidden => OperationResult<TripEligibility>.Failed(OperationOutcome.Forbidden),
                _ => OperationResult<TripEligibility>.Failed(OperationOutcome.SourceUnavailable)
            };
        }

        var loyaltyInfo = qualification.Value!;
        var entitlement = consoleOptions.Value.EffectiveTierEntitlements
            .FirstOrDefault(tier => string.Equals(tier.TierCode, loyaltyInfo.TierCode, StringComparison.OrdinalIgnoreCase));

        if (entitlement is null || entitlement.VehicleClasses.Count == 0)
        {
            return Rejected([new FieldIssue("qualification", $"Guest tier {loyaltyInfo.TierCode} is not eligible for console transfers")]);
        }

        var requestedClass = WireEnum.ToWire(request.VehicleClass);
        if (!entitlement.VehicleClasses.Contains(requestedClass, StringComparer.Ordinal))
        {
            return Rejected([new FieldIssue("vehicleClass", "Guest tier is not entitled to the selected vehicle class")]);
        }

        if (!loyaltyInfo.PlayVerified)
        {
            return Rejected([new FieldIssue("qualification", "Play history is not verified")]);
        }

        return null;
    }

    // 物业当日配额（D5）：已用量取本服务当日非取消行程数；未配置配额的物业不限制。
    private async Task<EligibilityVerdict> CheckQuotaAsync(
        TripEvaluationRequest request,
        CatalogLocation pickup,
        CatalogLocation dropoff,
        VehicleCapacity capacity,
        GuestProfile guest,
        CancellationToken cancellationToken)
    {
        var decision = await quotaGate.DecideAsync(request.PropertyId, request.PickupAt, cancellationToken);
        if (decision == QuotaDecision.Rejected)
        {
            return Verdict(Rejected([new FieldIssue("quota", "Daily quota for the property is exhausted")]), guest);
        }

        var eligibility = new TripEligibility(capacity, new TripRoute(
            pickup.LocationId, pickup.Name,
            dropoff.LocationId, dropoff.Name,
            CrossBorderRule.IsCrossBorder(pickup.Region, dropoff.Region)));

        return new EligibilityVerdict(OperationResult<TripEligibility>.Succeeded(eligibility), RequiresManagerReview: false, Guest: guest);
    }

    private async Task<OperationResult<CatalogLocation>> ReadLocationAsync(string locationId, CancellationToken cancellationToken)
    {
        var result = await catalog.GetLocationAsync(locationId, cancellationToken);

        return result.Outcome switch
        {
            DirectoryOutcome.Success => OperationResult<CatalogLocation>.Succeeded(result.Value!),
            DirectoryOutcome.NotFound => OperationResult<CatalogLocation>.Failed(OperationOutcome.NotFound),
            DirectoryOutcome.Forbidden => OperationResult<CatalogLocation>.Failed(OperationOutcome.Forbidden),
            _ => OperationResult<CatalogLocation>.Failed(OperationOutcome.SourceUnavailable)
        };
    }

    private static EligibilityVerdict Verdict(OperationResult<TripEligibility> failure, GuestProfile guest) => new(failure, RequiresManagerReview: false, Guest: guest);

    private static OperationResult<TripEligibility> FailureOf(OperationResult<CatalogLocation> source) =>
        OperationResult<TripEligibility>.Failed(source.Outcome);

    private static OperationResult<TripEligibility> Invalid(IReadOnlyList<FieldIssue> issues) =>
        OperationResult<TripEligibility>.Invalid(issues);

    private static OperationResult<TripEligibility> Rejected(IReadOnlyList<FieldIssue> issues) =>
        OperationResult<TripEligibility>.Rejected(issues);
}

// 一次建单/修改的评估输入。pickupAt 已按模式解析（ASAP 由调用方换算为具体时间）。
public sealed record TripEvaluationRequest(
    string GuestId,
    string PropertyId,
    string PickupLocationId,
    string DropoffLocationId,
    DateTimeOffset PickupAt,
    PickupMode PickupMode,
    VehicleClass VehicleClass,
    int Passengers,
    int Luggage);

// 校验通过的产物：车型容量与既定路线。
public sealed record TripEligibility(VehicleCapacity Capacity, TripRoute Route);

// 校验结论：失败结果 + 是否属于"资格不满足"（需要登记待经理复核的资格档案）+ 已读取的宾客（供登记与建单快照复用）。
public sealed record EligibilityVerdict(OperationResult<TripEligibility> Result, bool RequiresManagerReview, GuestProfile? Guest);
