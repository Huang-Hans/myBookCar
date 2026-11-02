using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Dispatch.Api.Contracts.Drivers;

/// <summary>
/// 部分更新司机的请求体；至少出现一个字段，未出现的键保持原值，数组整体替换，嵌入对象按 Patch 类型合并。
/// </summary>
/// <param name="Name">司机姓名；未传保持原值，非空白。</param>
/// <param name="LicenseNumber">驾照号码；未传保持原值，不区分大小写去重。</param>
/// <param name="LicenseExpiryDate">驾照到期业务日期；未传保持原值，YYYY-MM-DD。</param>
/// <param name="Phone">联系电话；未传保持原值，非空白。</param>
/// <param name="PropertyId">所属单一物业代码，对应 COD/SC/ALT/GH 等；未传保持原值。</param>
/// <param name="AssignedVehicleId">Vehicle ID; omitted keeps the binding, null clears it.</param>
/// <param name="Status">司机状态字面值，取 ACTIVE / INACTIVE / SUSPENDED；未传保持原值。</param>
/// <param name="Languages">语言列表整体替换；未传保持原值，显式 null 不合法。</param>
/// <param name="EmergencyContact">紧急联系人局部合并；未传保持原值，显式 null 不合法。</param>
public sealed record PatchDriverRequest(
    Optional<string?> Name,
    Optional<string?> LicenseNumber,
    Optional<string?> LicenseExpiryDate,
    Optional<string?> Phone,
    Optional<string?> PropertyId,
    Optional<string?> AssignedVehicleId,
    Optional<string?> Status,
    Optional<List<string?>?> Languages,
    Optional<EmergencyContactPatchRequest?> EmergencyContact);
