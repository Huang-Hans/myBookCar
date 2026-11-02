namespace LMS.Booking.Api.Contracts.Approvals;

/// <summary>
/// 审核材料槽位的当前状态；每种材料类型恰好一项。
/// </summary>
/// <param name="DocumentType">材料类型，取 PASSENGER_TRAVEL_DOCUMENT、VEHICLE_PERMIT、DRIVER_LICENSE 或 CUSTOMS_DECLARATION。</param>
/// <param name="IsVerified">管理端核验结果，true 为已核验（Verified）、false 为未核验（Missing）。</param>
/// <param name="FileId">已登记的文件标识；该槽位尚无文件引用时为空并省略该键。</param>
public sealed record ApprovalDocumentView(string DocumentType, bool IsVerified, string? FileId);
