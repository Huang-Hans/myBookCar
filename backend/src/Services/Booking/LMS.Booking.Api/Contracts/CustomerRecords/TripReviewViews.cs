namespace LMS.Booking.Api.Contracts.CustomerRecords;

/// <summary>
/// 行程评价。评价不改变行程状态、积分或司机等级。
/// </summary>
/// <param name="TripId">行程标识。</param>
/// <param name="TripReference">行程业务单号。</param>
/// <param name="Rating">评分，1～5。</param>
/// <param name="Comments">评语，最长 1000 个字符；没有时省略。</param>
/// <param name="CreatedAt">首次评价时间（UTC），修改时保持不变。</param>
/// <param name="UpdatedAt">最近修改时间（UTC）。</param>
/// <param name="Version">乐观锁版本号；首次保存为 1，修改时通过 X-Expected-Version 提交。</param>
public sealed record TripReviewView(
    string TripId,
    string TripReference,
    int Rating,
    string? Comments,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    long Version);

/// <summary>
/// 可评价（已完成）的本人行程及评价摘要。
/// </summary>
/// <param name="TripId">行程标识。</param>
/// <param name="TripReference">行程业务单号。</param>
/// <param name="ProductCode">服务产品；客服代订的订单没有产品，省略。</param>
/// <param name="PickupName">上车地点名称快照。</param>
/// <param name="DropoffName">下车地点名称快照。</param>
/// <param name="PickupAt">计划接车时间（UTC）。</param>
/// <param name="CompletedAt">行程结束时间（UTC）；旧订单没有记录时省略。</param>
/// <param name="CanReview">是否可以评价（已完成即可；评价窗口待业务确认，目前不设期限）。</param>
/// <param name="Review">已有评价；还没有评价时省略。</param>
public sealed record ReviewableTripView(
    string TripId,
    string TripReference,
    string? ProductCode,
    string PickupName,
    string DropoffName,
    DateTimeOffset PickupAt,
    DateTimeOffset? CompletedAt,
    bool CanReview,
    TripReviewView? Review);
