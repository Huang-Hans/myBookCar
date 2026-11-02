namespace LMS.Dispatch.Api.Contracts.Internal;

/// <summary>
/// 服务间契约：凭据核对结论。不包含密码哈希，也不区分失败原因。
/// </summary>
/// <param name="IsVerified">凭据是否核对通过。</param>
/// <param name="EntityId">核对通过时的司机标识（driverId）；否则省略。</param>
public sealed record CredentialVerificationView(bool IsVerified, string? EntityId);
