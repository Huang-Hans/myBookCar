namespace LMS.Booking.Application.Credentials.Handlers;

public interface ICheckCustomerLoginEligibilityHandler
{
    // 宾客记录仍存在且已设置密码时为 true。刷新令牌前由 Platform 调用。
    Task<bool> HandleAsync(string guestId, CancellationToken cancellationToken);
}
