namespace LMS.Booking.Api.Contracts.Previews;

/// <summary>不能提交的一条原因。</summary>
/// <param name="Code">GUEST_RESTRICTED / QUALIFICATION_NOT_MET / VEHICLE_NOT_ENTITLED / PASSENGERS_EXCEED_CAPACITY / LUGGAGE_EXCEEDS_CAPACITY / PICKUP_TIME_INVALID / QUOTA_EXHAUSTED / LOCATION_NOT_ACTIVE / RULE_VIOLATION。</param>
/// <param name="Field">对应的输入字段；整体性原因为空字符串。</param>
/// <param name="Message">可展示的英文说明。</param>
public sealed record BookingPreviewReasonView(string Code, string Field, string Message);
