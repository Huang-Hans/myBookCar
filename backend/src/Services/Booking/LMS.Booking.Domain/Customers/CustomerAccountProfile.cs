namespace LMS.Booking.Domain.Customers;

// 客户的本地补充资料，键为 guestId，与宾客快照（GuestProfile）分开保存，因此上游快照导入不会覆盖它。
// 只有客户可自行维护的字段：姓名（覆盖展示名）与邮箱。会员号、等级、积分、物业、登录手机号不在此列。
// 从未保存过的客户读到全空的默认（版本 0），不写库。
public sealed record CustomerAccountProfile(string GuestId, string? Name, string? Email, DateTimeOffset? UpdatedAt, long Version)
{
    public const int MaxNameLength = 100;
    public const int MaxEmailLength = 254;

    private const long FirstVersion = 1;

    public static CustomerAccountProfile Default(string guestId) => new(guestId, null, null, null, 0);

    public static CustomerAccountProfile Create(string guestId, string? name, string? email, DateTimeOffset now) =>
        new(guestId, name, email, now, FirstVersion);

    public CustomerAccountProfile With(string? name, string? email, DateTimeOffset now) =>
        this with { Name = name, Email = email, UpdatedAt = now };
}
