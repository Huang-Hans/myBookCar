using LMS.BuildingBlocks.Core.Authorization;

namespace LMS.Platform.Application.Authentication.Models;

public sealed record AppAccessTokenRequest(
    PrincipalType PrincipalType,
    string EntityId,
    string FamilyId,
    DateTimeOffset IssuedAt,
    TimeSpan Lifetime);
