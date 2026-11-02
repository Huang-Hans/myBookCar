using LMS.BuildingBlocks.Core.Authorization;

namespace LMS.Platform.Application.Authentication.Models;

public sealed record AppRefreshCommand(PrincipalType Type, string RefreshToken, string TraceId);
