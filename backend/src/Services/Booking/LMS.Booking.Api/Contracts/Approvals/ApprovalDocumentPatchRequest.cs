using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Booking.Api.Contracts.Approvals;

/// <summary>
/// 局部更新指定审核材料槽位的请求体；上传与人工核验互不替代，四类槽位以外的类型会被拒绝。
/// </summary>
/// <param name="FileId">上传完成后绑定的本审核、本类型材料文件标识；键未出现表示保持原绑定，显式 null 或任意文件不被接受。</param>
/// <param name="IsVerified">人工核验结果，true 为已核验、false 为取消核验；键未出现表示保持原值，显式 null 不被接受。</param>
public sealed record ApprovalDocumentPatchRequest(Optional<string?> FileId, Optional<bool?> IsVerified);
