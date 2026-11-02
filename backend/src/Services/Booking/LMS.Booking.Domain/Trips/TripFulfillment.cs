namespace LMS.Booking.Domain.Trips;

// 履约实际时间。全部为服务端收到并提交动作时的时间，未发生的步骤为 null，不回填计划时间。
// 计划接车 PickupAt 与实际接客 PickedUpAt 分开记录；InTransitAt / ArrivedAtDestinationAt 是 IN_PROGRESS 内的子阶段。
public sealed record TripFulfillment(
    DateTimeOffset? AcceptedAt,
    DateTimeOffset? EnRouteAt,
    DateTimeOffset? ArrivedAtPickupAt,
    DateTimeOffset? PickedUpAt,
    DateTimeOffset? InTransitAt,
    DateTimeOffset? ArrivedAtDestinationAt,
    DateTimeOffset? CompletedAt)
{
    public static TripFulfillment Empty { get; } = new(null, null, null, null, null, null, null);
}
