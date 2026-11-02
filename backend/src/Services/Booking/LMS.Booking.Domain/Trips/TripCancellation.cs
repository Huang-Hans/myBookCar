namespace LMS.Booking.Domain.Trips;

// 取消记录。reasonCode 必须来自 Platform 上传的取消原因清单（CF03），policyReference 仅为展示引用。
public sealed record TripCancellation(
    string ReasonCode,
    string Comments,
    string CancelledBy,
    DateTimeOffset CancelledAt,
    string? PolicyReference);
