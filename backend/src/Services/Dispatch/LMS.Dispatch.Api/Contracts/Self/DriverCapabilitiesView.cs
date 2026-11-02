namespace LMS.Dispatch.Api.Contracts.Self;

/// <summary>
/// 司机当前可执行的能力，由服务端按状态与资格给出，客户端不自行推断。
/// </summary>
/// <param name="CanChangeAvailability">是否可以切换工作状态；停用 / 暂停的司机或行程执行中为 false。</param>
/// <param name="CanAcceptOffers">是否可以接受派单邀请：司机启用且工作状态为 AVAILABLE。</param>
public sealed record DriverCapabilitiesView(bool CanChangeAvailability, bool CanAcceptOffers);
