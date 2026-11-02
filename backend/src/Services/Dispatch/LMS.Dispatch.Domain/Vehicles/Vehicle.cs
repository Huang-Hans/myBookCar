namespace LMS.Dispatch.Domain.Vehicles;

// 车辆主档。车牌在全库不区分大小写唯一。司机可空，空值表示未派车；一名司机至多指派一辆。
// 行程中目前由管理端写入。履约同步接入前，不会根据行程自动改状态。
public sealed record Vehicle(
    string VehicleId,
    string Plate,
    string Model,
    VehicleGrade Grade,
    string PropertyId,
    int Seats,
    int MileageKm,
    VehicleStatus Status,
    string? DriverId,
    long Version)
{
    private const string IdPrefix = "vehicle_";
    private const long FirstVersion = 1;

    public static Vehicle Create(
        string plate,
        string model,
        VehicleGrade grade,
        string propertyId,
        int seats,
        int mileageKm,
        VehicleStatus status,
        string? driverId) =>
        new(
            IdPrefix + Guid.CreateVersion7().ToString("N"),
            plate,
            model,
            grade,
            propertyId,
            seats,
            mileageKm,
            status,
            driverId,
            FirstVersion);
}
