namespace LMS.Booking.Domain.Trips;

// 司机可提交的履约动作（对应原型的六步进度，接受邀请即指派不在其中）。目标状态由服务端状态机决定，客户端不能提交任意状态。
public enum DriverProgressAction
{
    StartEnRoute,
    ArriveAtPickup,
    PickUpPassenger,
    StartTransit,
    ArriveAtDestination,
    CompleteTrip
}
