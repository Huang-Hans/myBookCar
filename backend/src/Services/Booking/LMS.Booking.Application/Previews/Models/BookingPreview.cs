namespace LMS.Booking.Application.Previews.Models;

// 无副作用的预订预览：不创建订单、不占配额、不扣积分，也不登记复审申请。创建订单时仍会重新校验。
public sealed record BookingPreview(
    bool CanSubmit,
    bool WillWaitlist,
    bool RequiresApproval,
    IReadOnlyList<BookingPreviewReason> BlockedReasons,
    BookingPreviewEligibility Eligibility,
    BookingPreviewPoints Points,
    DateTimeOffset EvaluatedAt);
