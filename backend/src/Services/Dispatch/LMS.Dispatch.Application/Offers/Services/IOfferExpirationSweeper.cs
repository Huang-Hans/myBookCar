namespace LMS.Dispatch.Application.Offers.Services;

// 把已过期的待响应邀请持久化为 EXPIRED 并释放占用；供后台定时任务调用。返回本次关闭的邀请数。
public interface IOfferExpirationSweeper
{
    Task<int> SweepAsync(CancellationToken cancellationToken);
}
