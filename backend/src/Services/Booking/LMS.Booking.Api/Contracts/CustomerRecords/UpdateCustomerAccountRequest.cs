using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Booking.Api.Contracts.CustomerRecords;

/// <summary>
/// 修改账户普通资料的请求体（PATCH，至少一个字段）。会员号、等级、积分、物业、登录手机号不可由客户修改，传入这些字段按未知字段拒绝。
/// 可选 X-Expected-Version：提供时必须等于当前版本（从未保存过为 0），否则 409。
/// </summary>
/// <param name="Name">姓名，非空，最长 100 字符；不可清空。只影响账户展示，不改写已有订单里的姓名快照。</param>
/// <param name="Email">邮箱，最长 254 字符；显式 null 或空白表示清除。</param>
public sealed record UpdateCustomerAccountRequest(Optional<string?> Name, Optional<string?> Email);
