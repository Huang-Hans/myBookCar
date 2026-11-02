namespace LMS.Booking.Application.Previews.Models;

// 资格摘要：等级、最低可参与等级、游玩核验，以及该等级可用车型和所选车型是否在其中。
public sealed record BookingPreviewEligibility(
    string Tier,
    string? TierMinimum,
    bool PlayVerified,
    int PlayWindowDays,
    IReadOnlyList<string> EntitledVehicleClasses,
    bool IsVehicleEntitled);
