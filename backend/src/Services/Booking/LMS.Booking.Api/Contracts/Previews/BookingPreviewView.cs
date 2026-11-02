namespace LMS.Booking.Api.Contracts.Previews;

/// <summary>
/// 预订预览结果。它是按当前输入的评估，不承诺之后仍有配额；创建订单时会重新校验。
/// </summary>
/// <param name="CanSubmit">本次输入是否可以提交创建订单。资格通过（qualified）不等于可提交，以此字段为准。</param>
/// <param name="WillWaitlist">兼容字段，恒为 false：订单一律以 CONFIRMED 受理，配额 WAITLIST 策略不再拦截。</param>
/// <param name="RequiresApproval">不能提交的原因是资格不满足、需要经理复审时为 true。预览本身不会登记复审申请。</param>
/// <param name="BlockedReasons">不能提交的全部已知原因；可提交时为空数组。</param>
/// <param name="Eligibility">资格摘要。</param>
/// <param name="Points">所选车型的积分预览。</param>
/// <param name="EvaluatedAt">评估时间（UTC）。</param>
public sealed record BookingPreviewView(
    bool CanSubmit,
    bool WillWaitlist,
    bool RequiresApproval,
    IReadOnlyList<BookingPreviewReasonView> BlockedReasons,
    BookingPreviewEligibilityView Eligibility,
    BookingPreviewPointsView Points,
    DateTimeOffset EvaluatedAt);
