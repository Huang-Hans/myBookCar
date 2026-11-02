namespace LMS.Dispatch.Domain.Vehicles;

public static class BookingVehicleGrades
{
    public static VehicleGrade? FromClass(string vehicleClass) => vehicleClass switch
    {
        "BUSINESS_SEDAN" => VehicleGrade.BusinessSedan,
        "LUXURY_SEDAN" => VehicleGrade.LuxurySedan,
        "LUXURY_MPV" => VehicleGrade.LuxuryMpv,
        _ => null
    };

    public static string ToClass(VehicleGrade grade) => grade switch
    {
        VehicleGrade.BusinessSedan => "BUSINESS_SEDAN",
        VehicleGrade.LuxurySedan => "LUXURY_SEDAN",
        VehicleGrade.LuxuryMpv => "LUXURY_MPV",
        _ => throw new ArgumentOutOfRangeException(nameof(grade))
    };
}
