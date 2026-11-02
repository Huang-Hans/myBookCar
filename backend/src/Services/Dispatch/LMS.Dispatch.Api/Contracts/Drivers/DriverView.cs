namespace LMS.Dispatch.Api.Contracts.Drivers;

/// <summary>
/// 司机详情响应体；映射 drivers 集合，可空字段为空时省略该键。
/// </summary>
/// <param name="DriverId">司机稳定标识，映射 driverId；不承诺原型演示编号的生产生成规则。</param>
/// <param name="Name">司机姓名。</param>
/// <param name="LicenseNumber">驾照号码，不区分大小写去重。</param>
/// <param name="LicenseExpiryDate">驾照到期业务日期，YYYY-MM-DD，不转换成午夜 UTC 时间点。</param>
/// <param name="Phone">联系电话。</param>
/// <param name="PropertyId">所属单一物业代码，对应 COD/SC/ALT/GH 等。</param>
/// <param name="AssignedVehicle">Read-only vehicle label resolved from assignedVehicleId; legacy labels remain readable.</param>
/// <param name="Status">司机状态字面值，取 ACTIVE / INACTIVE / SUSPENDED。</param>
/// <param name="Languages">语言列表，非空且去重。</param>
/// <param name="EmergencyContact">紧急联系人。</param>
/// <param name="Availability">司机可用状态字面值，取 AVAILABLE / ON_TRIP / OFF_DUTY / BREAK；只读字段，未接入可信同步来源前为空并省略该键，不默认伪造 OFF_DUTY。</param>
/// <param name="AvailabilitySource">可用状态的可信同步来源标签；只读字段，未接入可信同步来源前为空并省略该键。</param>
/// <param name="SyncedAt">可用状态最近同步时间，UTC 时间点（ISO 8601，Z 结尾）；只读字段，未接入可信同步来源前为空并省略该键。</param>
/// <param name="Restriction">司机黑名单展示信息，司机停用（INACTIVE）或暂停（SUSPENDED）时给出原因与生效日；无限制时省略该键。</param>
/// <param name="Version">乐观锁版本号，每次修改递增；PATCH 时通过 X-Expected-Version 头提交以做并发控制，不一致返回 409。</param>
public sealed record DriverView(
    string DriverId,
    string Name,
    string LicenseNumber,
    string LicenseExpiryDate,
    string Phone,
    string PropertyId,
    string AssignedVehicle,
    string Status,
    IReadOnlyList<string> Languages,
    EmergencyContactView EmergencyContact,
    string? Availability,
    string? AvailabilitySource,
    DateTime? SyncedAt,
    DriverRestrictionView? Restriction,
    long Version)
{
    public string? AssignedVehicleId { get; init; }
}
