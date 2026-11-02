namespace LMS.BuildingBlocks.Core.Authorization;

// 非员工调用人类型。对外字面值为 DRIVER / CUSTOMER；员工仍通过 portal_users 授权，不属于主体绑定。
public enum PrincipalType
{
    Driver,
    Customer
}
