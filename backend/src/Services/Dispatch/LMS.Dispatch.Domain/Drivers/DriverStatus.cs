namespace LMS.Dispatch.Domain.Drivers;

// 司机主档状态。对外字面值为 ACTIVE / INACTIVE / SUSPENDED；INACTIVE 表达停用，SUSPENDED 表达暂停，两者都进入司机黑名单。
public enum DriverStatus
{
    Active,
    Inactive,
    Suspended
}
