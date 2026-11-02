namespace LMS.Platform.Api.Contracts.Configuration;

// DailyQuota 来自 Booking 的权威配额，不取上传配置里的旧值；Booking 尚未提供时按合同省略。
/// <summary>
/// 物业配置视图；主体字段来自上传配置，每日配额来自 Booking 的权威值。
/// </summary>
/// <param name="PropertyId">物业代码。</param>
/// <param name="Name">物业名称。</param>
/// <param name="Address">物业地址。</param>
/// <param name="ContactPerson">联系人。</param>
/// <param name="ContactPhone">联系电话。</param>
/// <param name="AssignedFleetCount">分配的车队数量（>=0），不是车辆 ID 列表。</param>
/// <param name="OperatingHours">营业时间文本。</param>
/// <param name="ServiceLevel">服务等级文本。</param>
/// <param name="MinimumLeadTimeHours">最短预约提前量小时数（>=0）。</param>
/// <param name="CancellationPolicyReference">外部积分退还政策引用。</param>
/// <param name="MembershipEligibility">外部会员权益资格说明。</param>
/// <param name="DailyQuota">来自 property_quotas 的每日配额；Booking 未提供时为空并省略，不复制上传配置旧值。</param>
/// <param name="VipOverrideRule">VIP 例外规则说明，不是可执行脚本。</param>
public sealed record PropertyView(
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
    int? DailyQuota,
    string VipOverrideRule,
    bool IsActive,
    bool IsDeleted,
    long Version);
