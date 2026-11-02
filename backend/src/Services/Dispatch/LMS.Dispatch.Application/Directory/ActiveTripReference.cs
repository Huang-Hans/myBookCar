namespace LMS.Dispatch.Application.Directory;

// Booking 中司机当前有效（未终态）的指派行程引用。
public sealed record ActiveTripReference(string TripId, string TripReference);
