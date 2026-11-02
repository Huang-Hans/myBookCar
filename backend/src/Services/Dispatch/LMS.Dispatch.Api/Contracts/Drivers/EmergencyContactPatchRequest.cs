using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Dispatch.Api.Contracts.Drivers;

/// <summary>
/// 紧急联系人的局部更新；仅用于 PATCH 请求的 emergencyContact 键，未出现的键保持原值。
/// </summary>
/// <param name="Name">紧急联系人姓名；未传保持原值，显式 null 或空白会被拒绝。</param>
/// <param name="Phone">紧急联系人电话；未传保持原值，显式 null 或空白会被拒绝。</param>
public sealed record EmergencyContactPatchRequest(Optional<string?> Name, Optional<string?> Phone);
