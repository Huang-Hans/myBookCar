namespace LMS.Dispatch.Application.Offers.Services;

// 司机主档在接受提交期间被并发修改：本地事务应整体回滚，邀请保持处理中，司机用同一邀请重试即可（Booking 的指派以邀请标识幂等）。
internal sealed class DriverStateChangedException() : Exception("The driver record changed while the acceptance was being committed.");
