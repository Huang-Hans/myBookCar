namespace LMS.Dispatch.Api.Contracts.DispatchOrders;

/// <summary>
/// 司机下拉的候选。value 使用 driverId；显示名称与车辆文本不能替代 driverId 提交。
/// </summary>
/// <param name="DriverId">司机标识。</param>
/// <param name="Name">司机姓名。</param>
/// <param name="PropertyId">所属物业。</param>
/// <param name="Availability">可用状态，候选恒为 AVAILABLE。</param>
/// <param name="VehicleLabel">司机主档上的车辆文本（车型与车牌的自由文本）。</param>
/// <param name="VehicleClass">Current bound vehicle class matching the order.</param>
/// <param name="VehicleMatch">MATCHED: the enabled bound vehicle belongs to the trip property and its grade matches the order class.</param>
/// <param name="Languages">司机会讲的语言。</param>
public sealed record DispatchCandidateView(
    string DriverId,
    string Name,
    string PropertyId,
    string Availability,
    string VehicleLabel,
    string? VehicleClass,
    string VehicleMatch,
    IReadOnlyList<string> Languages);
