namespace LMS.Platform.Application.Authorization.Ports;

public interface IRolePermissionRepository
{
    // Null means the role is absent; an empty collection is an explicit denial of all permissions.
    Task<IReadOnlyCollection<string>?> FindPermissionsAsync(string roleCode, CancellationToken cancellationToken);
}
