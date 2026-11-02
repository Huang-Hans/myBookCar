using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Application.SelfService.Models;
using LMS.Booking.Application.SelfService.Options;
using LMS.Booking.Application.SelfService.Ports;
using LMS.Booking.Application.Trips.Services;
using LMS.Booking.Domain.Trips;
using Microsoft.Extensions.Options;

namespace LMS.Booking.Application.SelfService.Services;

// 自助建单的校验链（顺序即错误优先级）：产品可订 → 宾客级前置（物业 / 等级 / 黑名单）→ 时间窗 → 路线 → 车型与容量 → 物业配额。
// 规则全部在提交时重新计算，不信任先前的 ride-options 查询结论；任一外部源不可用一律 SourceUnavailable，不用默认值代替。
// 自助规则独立于客服：不使用客服的游玩核验、积分余额与经理复核流程，也不登记资格档案。
internal sealed class SelfServiceEligibilityService(
    GuestBookingGate gate,
    SelfServiceRouteChecker routeChecker,
    SelfServiceVehicleCatalog vehicles,
    PropertyQuotaGate quotaGate,
    IGuestTripRepository trips,
    IOptions<SelfServiceOptions> options,
    TimeProvider timeProvider)
{
    // ASAP 单的固定接车缓冲，与客服代订一致。
    private static readonly TimeSpan AsapBuffer = TimeSpan.FromMinutes(15);

    public async Task<OperationResult<SelfBookingApproval>> EvaluateAsync(SelfBookingRequest request, CancellationToken cancellationToken)
    {
        var product = options.Value.EffectiveProducts.FirstOrDefault(item => item.Code == request.ProductCode);
        if (product is null)
        {
            return OperationResult<SelfBookingApproval>.Invalid([new FieldIssue("productCode", "Unknown service product")]);
        }

        if (!product.IsEnabled)
        {
            return Rejected("productCode", product.UnavailableReason ?? "The service product is not available");
        }

        var context = await gate.LoadAsync(request.GuestId, cancellationToken);
        if (!context.IsSuccess)
        {
            return OperationResult<SelfBookingApproval>.FailureOf(context);
        }

        return context.Value!.Block is { } block
            ? Rejected("guest", $"Guest cannot book online: {WireEnum.ToWire(block)}")
            : await EvaluateBookableAsync(request, product, context.Value, cancellationToken);
    }

    private async Task<OperationResult<SelfBookingApproval>> EvaluateBookableAsync(
        SelfBookingRequest request,
        ServiceProductOption product,
        GuestBookingContext context,
        CancellationToken cancellationToken)
    {
        var timing = await CheckTimingAsync(request, context, cancellationToken);
        if (!timing.IsSuccess)
        {
            return OperationResult<SelfBookingApproval>.FailureOf(timing);
        }

        var route = await routeChecker.CheckAsync(product, request.PickupLocationId, request.DropoffLocationId, cancellationToken);
        if (!route.IsSuccess)
        {
            return OperationResult<SelfBookingApproval>.FailureOf(route);
        }

        var capacity = vehicles.CheckSelection(context.Guest, request.VehicleClass, request.Passengers, request.Luggage);

        return capacity.IsSuccess
            ? await CheckQuotaAsync(context, product, route.Value!.Route, capacity.Value!, timing.Value!.PickupAt, cancellationToken)
            : OperationResult<SelfBookingApproval>.FailureOf(capacity);
    }

    // 时间窗：ASAP 须部署启用；预约须满足 Platform 的最短提前量与最远预订天数；同一天的活跃订单数不得超过规则上限。
    private async Task<OperationResult<ResolvedPickup>> CheckTimingAsync(
        SelfBookingRequest request,
        GuestBookingContext context,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var resolved = ResolvePickupAt(request, now);
        if (!resolved.IsSuccess)
        {
            return resolved;
        }

        var window = CheckWindow(request.PickupMode, resolved.Value!.PickupAt, now, context.Rules);
        if (window is not null)
        {
            return window;
        }

        var (dayStart, dayEnd) = MacauClock.DayWindow(MacauClock.DateOf(resolved.Value.PickupAt));
        var active = await trips.CountActiveForGuestAsync(context.Guest.GuestId, dayStart, dayEnd, cancellationToken);

        return active >= context.Rules.MaximumActiveRequestsPerGuestPerDay
            ? TimingRejected("pickupAt", "The daily limit of active bookings has been reached")
            : resolved;
    }

    private OperationResult<ResolvedPickup> ResolvePickupAt(SelfBookingRequest request, DateTimeOffset now)
    {
        if (request.PickupMode == PickupMode.Asap)
        {
            return options.Value.AsapEnabled
                ? OperationResult<ResolvedPickup>.Succeeded(new ResolvedPickup(now.Add(AsapBuffer)))
                : TimingRejected("pickupMode", "Immediate pickup is not available; schedule the pickup in advance");
        }

        return request.PickupAt is { } pickupAt
            ? OperationResult<ResolvedPickup>.Succeeded(new ResolvedPickup(pickupAt))
            : OperationResult<ResolvedPickup>.Invalid([new FieldIssue("pickupAt", "Required unless pickupMode is ASAP")]);
    }

    private static OperationResult<ResolvedPickup>? CheckWindow(
        PickupMode mode,
        DateTimeOffset pickupAt,
        DateTimeOffset now,
        Ports.SelfBookingRules rules)
    {
        if (mode != PickupMode.Asap && pickupAt < now.AddHours(rules.MinimumLeadTimeHours))
        {
            return TimingRejected("pickupAt", $"Must be at least {rules.MinimumLeadTimeHours} hours ahead");
        }

        return pickupAt > now.AddDays(rules.MaximumAdvanceDays)
            ? TimingRejected("pickupAt", $"Must be within {rules.MaximumAdvanceDays} days")
            : null;
    }

    private async Task<OperationResult<SelfBookingApproval>> CheckQuotaAsync(
        GuestBookingContext context,
        ServiceProductOption product,
        TripRoute route,
        VehicleCapacity capacity,
        DateTimeOffset pickupAt,
        CancellationToken cancellationToken)
    {
        var decision = await quotaGate.DecideAsync(context.Guest.PropertyId!, pickupAt, cancellationToken);
        if (decision == QuotaDecision.Rejected)
        {
            return Rejected("quota", "Daily quota for the property is exhausted");
        }

        return OperationResult<SelfBookingApproval>.Succeeded(new SelfBookingApproval(context.Guest, product, route, pickupAt, capacity));
    }

    private static OperationResult<SelfBookingApproval> Rejected(string field, string message) =>
        OperationResult<SelfBookingApproval>.Rejected([new FieldIssue(field, message)]);

    private static OperationResult<ResolvedPickup> TimingRejected(string field, string message) =>
        OperationResult<ResolvedPickup>.Rejected([new FieldIssue(field, message)]);
}
