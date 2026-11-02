namespace LMS.Dispatch.Api.Contracts.Self;

/// <summary>
/// 司机本人资料与工作状态。只包含允许本人看到的字段，不含驾照号、紧急联系人等管理字段。
/// </summary>
/// <param name="DriverId">司机标识，来自令牌绑定。</param>
/// <param name="Name">司机姓名。</param>
/// <param name="PropertyId">所属物业代码。</param>
/// <param name="AssignedVehicle">分配车辆文本；没有分配时为空字符串。</param>
/// <param name="Languages">语言列表。</param>
/// <param name="Status">司机主档状态 ACTIVE / INACTIVE / SUSPENDED，与工作状态分开。</param>
/// <param name="Availability">工作状态 AVAILABLE / ON_TRIP / OFF_DUTY / BREAK；尚未上报时省略。</param>
/// <param name="AvailabilitySource">工作状态来源：DRIVER_SELF_REPORT / TRIP_ASSIGNMENT / TRIP_RECONCILIATION；尚未上报时省略。</param>
/// <param name="SyncedAt">工作状态最近写入时间（UTC）；尚未上报时省略。</param>
/// <param name="Capabilities">当前可执行的能力。</param>
/// <param name="Version">乐观锁版本号（只读展示，切换工作状态不需要提交版本）。</param>
public sealed record DriverSelfView(
    string DriverId,
    string Name,
    string PropertyId,
    string AssignedVehicle,
    IReadOnlyList<string> Languages,
    string Status,
    string? Availability,
    string? AvailabilitySource,
    DateTime? SyncedAt,
    DriverCapabilitiesView Capabilities,
    long Version);
