namespace LMS.Booking.Api.Contracts.Trips;

/// <summary>
/// 最近一次修改记录（原型的 “Last modification reason”）。
/// </summary>
/// <param name="Reason">GUEST_REQUEST / SCHEDULE_CHANGE；调用方当时没有提供原因时省略。</param>
/// <param name="Comments">修改备注；没有时省略。</param>
/// <param name="ModifiedBy">操作人用户标识。</param>
/// <param name="ModifiedByName">操作人姓名快照；早于该字段的记录省略。</param>
/// <param name="ModifiedAt">修改提交时间（UTC，服务端记录）。</param>
/// <param name="ChangedFields">本次实际改动的字段名（不含字段值）。</param>
public sealed record TripModificationView(
    string? Reason,
    string? Comments,
    string ModifiedBy,
    string? ModifiedByName,
    DateTimeOffset ModifiedAt,
    IReadOnlyList<string> ChangedFields);
