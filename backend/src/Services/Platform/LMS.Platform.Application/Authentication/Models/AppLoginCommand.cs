using LMS.BuildingBlocks.Core.Authorization;

namespace LMS.Platform.Application.Authentication.Models;

// 登录命令。身份类型由登录入口固定，不来自请求正文。
public sealed record AppLoginCommand(PrincipalType Type, string Phone, string Password, string TraceId);
