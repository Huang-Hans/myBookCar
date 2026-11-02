namespace LMS.Booking.Domain.Trips;

// 行程单（客服代订 HA-01/HA-03 与客户自助预订共用）。状态机见 TripTransitions；积分只记引用（TripPoints），
// 代订人取自身份解析（客户自助时为客户本人）。Assignment / Fulfillment / Events 记录权威指派与履约事实：
// 存量行程没有这些字段，读取时为空指派、空履约时间与空轨迹，不推断司机或实际时间。
public sealed record Trip(
    string TripId,
    string TripReference,
    string PropertyId,
    TripGuest Guest,
    TripRoute Route,
    DateTimeOffset PickupAt,
    PickupMode PickupMode,
    ServiceType ServiceType,
    VehicleClass VehicleClass,
    int Passengers,
    int Luggage,
    string? SpecialRequests,
    TripStatus Status,
    TripPoints Points,
    TripCancellation? Cancellation,
    BookedBy BookedBy,
    string ConfirmationLanguage,
    DateTimeOffset CreatedAt,
    TripChannel Channel,
    ServiceProduct? Product,
    DriverAssignment? Assignment,
    TripFulfillment Fulfillment,
    IReadOnlyList<TripEvent> Events,
    DateTimeOffset? ClosedAt,
    long Version,
    IReadOnlyList<TripModification>? Modifications = null,
    string? CouponId = null,
    string? CouponRedemptionKey = null)
{
    public const string IdPrefix = "trip_";
    public const string ReferencePrefix = "TR-";

    private const long FirstVersion = 1;

    // 最近一次修改；从未修改或存量行程为空。
    public TripModification? LastModification => Modifications?.LastOrDefault();

    public bool IsTerminal => Status is TripStatus.Completed or TripStatus.Cancelled;

    // 已确认的行程可指派；存量 PENDING 行程同样可指派。已有指派、进行中与终态不可重复指派。
    public bool CanBeAssigned => Status is (TripStatus.Pending or TripStatus.Confirmed) && Assignment is null;

    // 客服代订与客户自助建单均直接进入 CONFIRMED；“待派单”由 Dispatch 的派单状态 PENDING 表达，不再产生订单 PENDING。
    public static Trip Create(
        string tripReference,
        string propertyId,
        TripGuest guest,
        TripRoute route,
        DateTimeOffset pickupAt,
        PickupMode pickupMode,
        ServiceType serviceType,
        VehicleClass vehicleClass,
        int passengers,
        int luggage,
        string? specialRequests,
        long pointsRequired,
        BookedBy bookedBy,
        string confirmationLanguage,
        DateTimeOffset now,
        TripChannel channel = TripChannel.Console,
        ServiceProduct? product = null,
        string? couponId = null,
        string? couponRedemptionKey = null) =>
        new(
            IdPrefix + Guid.CreateVersion7().ToString("N"),
            tripReference, propertyId, guest, route,
            pickupAt, pickupMode, serviceType, vehicleClass, passengers, luggage, specialRequests,
            TripStatus.Confirmed,
            TripPoints.ReservedFor(pointsRequired), null, bookedBy, confirmationLanguage, now,
            channel, product, null, TripFulfillment.Empty, [], null, FirstVersion, null, couponId, couponRedemptionKey);

    // 取消：前置校验（CanCancel）由调用方完成；积分引用回到 RETURNED；已指派的行程保留指派，仍归属原司机的已取消历史。
    public Trip Cancel(TripCancellation cancellation) =>
        this with
        {
            Status = TripStatus.Cancelled,
            Cancellation = cancellation,
            Points = Points.AsReturned(),
            ClosedAt = cancellation.CancelledAt,
            Events = [.. Events, TripEvent.Create(TripEventType.Cancelled, TripStatus.Cancelled, cancellation.CancelledBy, cancellation.CancelledAt, null)]
        };

    // 修改：路由与时间由应用层解析后整体替换，字段级校验见 TripRules。
    public Trip Update(
        TripRoute route,
        DateTimeOffset pickupAt,
        PickupMode pickupMode,
        ServiceType serviceType,
        VehicleClass vehicleClass,
        int passengers,
        int luggage,
        string? specialRequests,
        string confirmationLanguage,
        long pointsRequired) =>
        this with
        {
            Route = route,
            PickupAt = pickupAt,
            PickupMode = pickupMode,
            ServiceType = serviceType,
            VehicleClass = vehicleClass,
            Passengers = passengers,
            Luggage = luggage,
            SpecialRequests = specialRequests,
            ConfirmationLanguage = confirmationLanguage,
            Points = Points with { Required = pointsRequired }
        };

    // 追加一条修改记录（调用方在 Update 之后调用），保留此前全部修改。
    public Trip WithModification(TripModification modification) =>
        this with { Modifications = [.. Modifications ?? [], modification] };

    // 状态推进（/internal）：终态校验由调用方完成；完成时积分引用转为 REDEEMED。
    public Trip AdvanceTo(TripStatus target) =>
        target == TripStatus.Completed ? this with { Status = target, Points = Points.AsRedeemed() } : this with { Status = target };

    // 权威指派：前置校验（CanBeAssigned）由调用方完成。接单即进入 DRIVER_ASSIGNED 并记录接受时间，不默认等于已出发。
    public Trip AssignDriver(DriverAssignment assignment) =>
        this with
        {
            Status = TripStatus.DriverAssigned,
            Assignment = assignment,
            Fulfillment = Fulfillment with { AcceptedAt = assignment.AssignedAt },
            Events = [.. Events, TripEvent.Create(TripEventType.DriverAssigned, TripStatus.DriverAssigned, assignment.DriverId, assignment.AssignedAt, null)]
        };

    // 应用一个司机履约动作：调用方须确认 action 等于 FulfillmentRules.NextAction。完成时积分引用转为 REDEEMED 并记录结束时间。
    public Trip ApplyProgress(DriverProgressAction action, string actor, DateTimeOffset now, string idempotencyKey)
    {
        var step = FulfillmentRules.StepFor(action, Fulfillment, now);
        var updated = this with
        {
            Status = step.Status,
            Fulfillment = step.Fulfillment,
            Events = [.. Events, TripEvent.Create(step.Type, step.Status, actor, now, idempotencyKey)]
        };

        return step.Status == TripStatus.Completed ? updated with { Points = Points.AsRedeemed(), ClosedAt = now } : updated;
    }
}
