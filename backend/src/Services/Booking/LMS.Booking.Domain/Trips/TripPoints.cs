namespace LMS.Booking.Domain.Trips;

// 积分引用：所需积分与外部兑换引用。状态的转移只随行程状态机发生（创建→RESERVED，完成→REDEEMED，取消→RETURNED）。
public sealed record TripPoints(long Required, string? Reference, PointsState State)
{
    public static TripPoints ReservedFor(long required) => new(required, null, PointsState.Reserved);

    public TripPoints AsRedeemed() => State == PointsState.Reserved ? this with { State = PointsState.Redeemed } : this;

    public TripPoints AsReturned() => State == PointsState.Reserved ? this with { State = PointsState.Returned } : this;
}
