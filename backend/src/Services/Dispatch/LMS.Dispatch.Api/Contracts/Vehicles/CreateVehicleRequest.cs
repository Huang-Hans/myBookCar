namespace LMS.Dispatch.Api.Contracts.Vehicles;

/// <summary>
/// 新增车辆。对应车辆列表中的车牌、车型、等级、物业、座位、里程、状态，以及可选的指派司机。
/// </summary>
/// <param name="Plate">车牌。全库不区分大小写唯一。</param>
/// <param name="Model">车型，例如 Toyota Alphard。</param>
/// <param name="Grade">等级字面值，取 BUSINESS_SEDAN / LUXURY_MPV / LUXURY_SEDAN，对应普通轿车、豪华七人车、豪华轿车。</param>
/// <param name="PropertyId">物业代码，例如 COD / SC / ALT / GH。必须在调用者授权范围内。</param>
/// <param name="Seats">座位数，正整数。</param>
/// <param name="MileageKm">里程，单位公里，大于等于 0。</param>
/// <param name="Status">状态字面值，取 AVAILABLE / ON_TRIP / MAINTENANCE / OUT_OF_SERVICE，对应可用、行程中、维修中、停用。省略时按可用。</param>
/// <param name="DriverId">指派司机。省略表示未派车。司机必须启用，且尚未指派到其他车辆。</param>
public sealed record CreateVehicleRequest(
    string? Plate,
    string? Model,
    string? Grade,
    string? PropertyId,
    int? Seats,
    int? MileageKm,
    string? Status,
    string? DriverId);
