namespace LMS.Booking.Api.Contracts.Qualifications;

/// <summary>
/// 客人与会员等级快照：资格参考和跨境审批共用的客人信息。
/// </summary>
/// <param name="GuestId">外部会员唯一标识。</param>
/// <param name="Name">客人显示姓名快照。</param>
/// <param name="TierCode">上传的会员等级代码；不在本地重新评定等级。</param>
public sealed record GuestSnapshotView(string GuestId, string Name, string TierCode);
