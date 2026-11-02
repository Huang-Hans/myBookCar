using LMS.Booking.Domain.Customers;
using LMS.Booking.Domain.Guests;

namespace LMS.Booking.Application.Customers.Models;

// 客户账户视图的数据：宾客快照（会员号、等级、积分、登录手机号等上游事实）加本地补充资料。
// 展示姓名以本地姓名为准，没有则回退到快照姓名；登录手机号只读，不在普通资料里修改。
public sealed record CustomerAccount(GuestProfile Guest, CustomerAccountProfile Local)
{
    public string DisplayName => Local.Name ?? Guest.Name;

    public bool IsNameOverridden => Local.Name is not null;
}
