using LMS.BuildingBlocks.Core.Patching;
using LMS.Booking.Application.Abstractions;

namespace LMS.Booking.Application.Customers.Commands;

// 客户本人修改普通资料。Name 不可清空；Email 显式 null 表示清除。登录手机号不在此命令内。ExpectedVersion 为 0 表示不比较版本。
public sealed record UpdateCustomerAccountCommand(
    string GuestId,
    Optional<string> Name,
    Optional<string> Email,
    long ExpectedVersion,
    CommandContext Context);
