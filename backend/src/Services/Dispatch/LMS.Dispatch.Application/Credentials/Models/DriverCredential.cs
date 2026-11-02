using LMS.Dispatch.Domain.Drivers;

namespace LMS.Dispatch.Application.Credentials.Models;

// 司机登录所需的最小凭据视图：只供凭据核对使用，不是司机资料，也不会出现在任何 API 响应中。
public sealed record DriverCredential(string DriverId, string? PasswordHash, DriverStatus Status);
