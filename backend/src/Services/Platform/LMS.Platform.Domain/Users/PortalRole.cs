namespace LMS.Platform.Domain.Users;

// 应用用户的单一角色。对外字面值为 SUPER_ADMIN / OPERATIONS_MANAGER / DISPATCHER / FINANCE / PROPERTY_CS_STAFF。
public enum PortalRole
{
    SuperAdmin,
    OperationsManager,
    Dispatcher,
    Finance,
    PropertyCsStaff
}
