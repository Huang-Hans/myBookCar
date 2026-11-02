using LMS.BuildingBlocks.Core.Authorization;

namespace LMS.BuildingBlocks.Identity.Authorization;

internal sealed class TokenOnlyPrincipalRecordVerifier : IPrincipalRecordVerifier
{
    public Task<bool> IsUsableAsync(PrincipalType type, string entityId, CancellationToken cancellationToken) => Task.FromResult(true);
}
