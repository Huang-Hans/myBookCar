namespace LMS.Booking.Infrastructure.Remote;

internal sealed record RemoteFilePayload(string FileId, string? ApprovalId, string? DocumentType, string ScanStatus);
