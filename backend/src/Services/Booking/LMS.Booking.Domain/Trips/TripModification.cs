namespace LMS.Booking.Domain.Trips;

// 一次修改记录：服务端提交时间、操作者与实际改动的字段名（不复制字段值）。
// Reason 对未提供原因的调用方为空，不用默认原因代替。
public sealed record TripModification(
    TripModificationReason? Reason,
    string? Comments,
    string ModifiedBy,
    string? ModifiedByName,
    DateTimeOffset ModifiedAt,
    IReadOnlyList<string> ChangedFields);
