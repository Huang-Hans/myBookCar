namespace LMS.Dispatch.Api.Contracts.Drivers;

// 合同 CreateDriverRequest。可用状态、同步来源和限制信息是只读字段，请求中出现即为未知字段。
/// <summary>
/// 新增司机的请求体；仅原型表单可编辑字段，未知字段拒绝。
/// </summary>
/// <param name="Name">司机姓名；必填，非空白。</param>
/// <param name="LicenseNumber">驾照号码；必填，非空白，不区分大小写去重。</param>
/// <param name="LicenseExpiryDate">驾照到期业务日期；必填，YYYY-MM-DD。</param>
/// <param name="Phone">联系电话；必填，非空白。</param>
/// <param name="PropertyId">所属单一物业代码，对应 COD/SC/ALT/GH 等；必填，非空白。</param>
/// <param name="AssignedVehicleId">Vehicle ID; null or omitted means unassigned.</param>
/// <param name="Status">司机状态字面值；必填，取 ACTIVE / INACTIVE / SUSPENDED。</param>
/// <param name="Languages">语言列表；必填，显式转换为非空且去重的字符串列表。</param>
/// <param name="EmergencyContact">紧急联系人；必填，姓名与电话均非空白。</param>
public sealed record CreateDriverRequest(
    string? Name,
    string? LicenseNumber,
    string? LicenseExpiryDate,
    string? Phone,
    string? PropertyId,
    string? AssignedVehicleId,
    string? Status,
    List<string?>? Languages,
    EmergencyContactRequest? EmergencyContact);
