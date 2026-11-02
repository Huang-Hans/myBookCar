namespace LMS.BuildingBlocks.Http.Contracts;

/// <summary>
/// 网关横幅：服务标识与用途说明，用于确认网关已就绪。
/// </summary>
/// <param name="ServiceName">服务名，取自运行环境。</param>
/// <param name="Description">服务用途说明。</param>
public sealed record ServiceBannerResponse(string ServiceName, string Description);
