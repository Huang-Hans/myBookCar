using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Application.Console.Options;
using LMS.Booking.Application.Directory;
using LMS.Booking.Application.Guests.Ports;
using LMS.Booking.Application.Previews.Models;
using LMS.Booking.Application.Previews.Services;
using LMS.Booking.Application.Trips.Handlers;
using LMS.Booking.Application.Trips.Services;
using LMS.Booking.Domain.Guests;
using Microsoft.Extensions.Options;

namespace LMS.Booking.Application.Previews.Handlers;

// 预订预览：复用建单的完整校验链（TripEligibilityService），所以预览与创建订单使用同一套规则，不另写一份报价常量。
// 校验链本身不写任何数据，资格不满足时由建单 / 检查接口另行登记复审申请，这里不登记。
internal sealed class BookingPreviewHandler(
    TripEligibilityService eligibility,
    ILoyaltyGateway loyalty,
    IOptions<ConsoleOptions> consoleOptions,
    TimeProvider timeProvider,
    ConsoleBookingProductRules products) : IBookingPreviewHandler
{
    private readonly VehicleCapacityResolver capacityResolver = new(consoleOptions);

    public async Task<OperationResult<BookingPreview>> HandleAsync(BookingPreviewQuery query, CancellationToken cancellationToken)
    {
        if (!query.Context.Scope.Allows(query.PropertyId))
        {
            return OperationResult<BookingPreview>.Failed(OperationOutcome.Forbidden);
        }

        var now = timeProvider.GetUtcNow();
        var pickupAt = query.PickupMode == Domain.Trips.PickupMode.Asap ? now.Add(CreateTripHandler.AsapBuffer) : query.PickupAt!.Value;
        var verdict = await eligibility.EvaluateAsync(ToRequest(query, pickupAt), cancellationToken);
        if (verdict.Result.IsSuccess && query.Product is { } product)
        {
            var selection = await products.CheckAsync(product, query.PickupLocationId, query.DropoffLocationId, query.PickupMode, cancellationToken);
            if (!selection.IsSuccess) verdict = new EligibilityVerdict(OperationResult<TripEligibility>.FailureOf(selection), false, verdict.Guest);
        }
        if (IsSourceFailure(verdict.Result.Outcome))
        {
            return OperationResult<BookingPreview>.FailureOf(verdict.Result);
        }

        var qualification = await loyalty.GetQualificationAsync(query.GuestId, cancellationToken);

        return qualification.IsSuccess
            ? OperationResult<BookingPreview>.Succeeded(Compose(query, verdict, qualification.Value!, now))
            : OperationResult<BookingPreview>.Failed(FailureOf(qualification.Outcome));
    }

    // 订单不再以候补受理，WillWaitlist 恒为 false，契约字段保留以兼容客户端。
    private BookingPreview Compose(BookingPreviewQuery query, EligibilityVerdict verdict, LoyaltyQualification loyaltyInfo, DateTimeOffset now) => new(
        verdict.Result.IsSuccess,
        false,
        verdict.RequiresManagerReview,
        verdict.Result.Issues.Select(BookingPreviewReasons.From).ToList(),
        EligibilityOf(query, loyaltyInfo),
        PointsOf(query, loyaltyInfo),
        now);

    private BookingPreviewEligibility EligibilityOf(BookingPreviewQuery query, LoyaltyQualification loyaltyInfo)
    {
        var entitled = TierEntitlementLookup.VehicleClassesOf(consoleOptions.Value, loyaltyInfo.TierCode);

        return new BookingPreviewEligibility(
            loyaltyInfo.TierCode,
            TierEntitlementLookup.TierMinimum(consoleOptions.Value),
            loyaltyInfo.PlayVerified,
            loyaltyInfo.PlayWindowDays,
            entitled,
            entitled.Contains(WireEnum.ToWire(query.VehicleClass), StringComparer.Ordinal));
    }

    private BookingPreviewPoints PointsOf(BookingPreviewQuery query, LoyaltyQualification loyaltyInfo)
    {
        var required = capacityResolver.CapacityOf(query.VehicleClass).PointsRequired;
        var available = loyaltyInfo.PointsBalance;

        return new BookingPreviewPoints(
            required,
            available,
            available is null ? null : Math.Max(0, required - available.Value),
            available is null ? null : available.Value - required);
    }

    private static TripEvaluationRequest ToRequest(BookingPreviewQuery query, DateTimeOffset pickupAt) => new(
        query.GuestId, query.PropertyId, query.PickupLocationId, query.DropoffLocationId,
        pickupAt, query.PickupMode, query.VehicleClass, query.Passengers, query.Luggage);

    // 来源不可用、越权、对象不存在是调用失败；业务规则不满足（BusinessRuleRejected / ValidationFailed）是预览的内容。
    private static bool IsSourceFailure(OperationOutcome outcome) =>
        outcome is OperationOutcome.NotFound or OperationOutcome.Forbidden or OperationOutcome.SourceUnavailable;

    private static OperationOutcome FailureOf(DirectoryOutcome outcome) => outcome switch
    {
        DirectoryOutcome.NotFound => OperationOutcome.NotFound,
        DirectoryOutcome.Forbidden => OperationOutcome.Forbidden,
        _ => OperationOutcome.SourceUnavailable
    };
}
