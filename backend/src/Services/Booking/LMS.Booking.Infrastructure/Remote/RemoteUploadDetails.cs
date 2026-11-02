namespace LMS.Booking.Infrastructure.Remote;

internal sealed record RemoteUploadDetails(string Url, string Method, string ContentType, DateTime ExpiresAt);
