namespace LMS.Dispatch.Infrastructure.Booking;

internal sealed record DispatchOrderCountsPayload(
    long PendingConfirmation,
    long Confirmed,
    long Assigned,
    long Completed,
    long Cancelled,
    long ConfirmedAwaiting,
    long ConfirmedProcessing,
    long ConfirmedRedispatch);
