namespace LMS.Dispatch.Api.Contracts.Vehicles;

/// <summary>
/// 一辆车。driverName 由司机主档补齐；未派车或主档已不存在时省略。
/// </summary>
/// <param name="VehicleId">车辆标识。</param>
/// <param name="Plate">车牌。</param>
/// <param name="Model">车型。</param>
/// <param name="Grade">等级字面值，取 BUSINESS_SEDAN / LUXURY_MPV / LUXURY_SEDAN。</param>
/// <param name="PropertyId">物业代码。地点名称由物业目录解析。</param>
/// <param name="Seats">座位数。</param>
/// <param name="MileageKm">里程，单位公里。</param>
/// <param name="Status">状态字面值，取 AVAILABLE / ON_TRIP / MAINTENANCE / OUT_OF_SERVICE。</param>
/// <param name="DriverId">指派司机。未派车时省略。</param>
/// <param name="DriverName">司机姓名。未派车或主档已不存在时省略。</param>
/// <param name="Version">乐观锁版本号。</param>
public sealed record VehicleView(
    string VehicleId,
    string Plate,
    string Model,
    string Grade,
    string PropertyId,
    int Seats,
    int MileageKm,
    string Status,
    string? DriverId,
    string? DriverName,
    long Version);
