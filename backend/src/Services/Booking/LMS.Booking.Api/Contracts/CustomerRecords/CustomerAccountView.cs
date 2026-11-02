namespace LMS.Booking.Api.Contracts.CustomerRecords;

/// <summary>
/// 客户账户资料与会员摘要。会员号、等级、积分来自可信的会员快照，只读；没有可信积分时 pointsBalance 省略，不返回示例数值。
/// </summary>
/// <param name="GuestId">客户（宾客）标识，来自令牌，不可修改。</param>
/// <param name="Name">展示姓名：本地资料里保存的姓名优先，没有则取会员快照姓名。</param>
/// <param name="NameSource">姓名来源：LOCAL（客户自行保存）/ MEMBER_SNAPSHOT（会员快照）。</param>
/// <param name="Email">邮箱；没有保存时省略。</param>
/// <param name="PhoneMasked">登录手机号（掩码）。手机号只读，修改需要独立的验证流程，尚未开放。</param>
/// <param name="IsPhoneChangeAvailable">是否可以修改登录手机号；改号验证渠道与规则（Q2-03）未确认前恒为 false。</param>
/// <param name="MemberId">会员号（快照，只读）。</param>
/// <param name="TierCode">会员等级代码（快照，只读）。</param>
/// <param name="PointsBalance">积分余额（快照，只读，可能不是实时余额）；没有可信值时省略。</param>
/// <param name="MembershipSource">会员信息来源：MEMBER_SNAPSHOT。</param>
/// <param name="UpdatedAt">本地补充资料最近保存时间（UTC）；从未保存过时省略。</param>
/// <param name="Version">本地补充资料版本号；从未保存过为 0，修改时可通过 X-Expected-Version 提交。</param>
public sealed record CustomerAccountView(
    string GuestId,
    string Name,
    string NameSource,
    string? Email,
    string? PhoneMasked,
    bool IsPhoneChangeAvailable,
    string MemberId,
    string TierCode,
    long? PointsBalance,
    string MembershipSource,
    DateTimeOffset? UpdatedAt,
    long Version);
