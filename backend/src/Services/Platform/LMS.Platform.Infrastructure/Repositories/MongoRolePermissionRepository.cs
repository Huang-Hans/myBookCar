using LMS.Platform.Application.Authorization.Ports;
using LMS.Platform.Infrastructure.Documents;
using LMS.Platform.Infrastructure.Persistence;
using MongoDB.Driver;

namespace LMS.Platform.Infrastructure.Repositories;

internal sealed class MongoRolePermissionRepository(IMongoDatabase database) : IRolePermissionRepository
{
    public async Task<IReadOnlyCollection<string>?> FindPermissionsAsync(string roleCode, CancellationToken cancellationToken)
    {
        var collection = database.GetCollection<RolePermissionDocument>(PlatformCollections.RolePermissions);
        var document = await collection.Find(candidate => candidate.RoleCode == roleCode).FirstOrDefaultAsync(cancellationToken);

        return document is null ? null : document.Permissions ?? [];
    }
}
