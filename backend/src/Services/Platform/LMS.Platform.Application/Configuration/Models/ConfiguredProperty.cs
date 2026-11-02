namespace LMS.Platform.Application.Configuration.Models;

// 上传配置中的物业。每日限额不属于上传配置，由 Booking 的权威配额提供，这里不保存。
public sealed record ConfiguredProperty(
    string PropertyId,
    string Name,
    string Address,
    string ContactPerson,
    string ContactPhone,
    int AssignedFleetCount,
    string OperatingHours,
    string ServiceLevel,
    int MinimumLeadTimeHours,
    string CancellationPolicyReference,
    string MembershipEligibility,
    string VipOverrideRule,
    bool IsActive = true,
    bool IsDeleted = false,
    long Version = 1);
