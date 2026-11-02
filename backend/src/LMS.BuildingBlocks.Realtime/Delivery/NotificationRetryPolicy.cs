using LMS.BuildingBlocks.Core.Notifications.Delivery;

namespace LMS.BuildingBlocks.Realtime.Delivery;

// 指数退避：第 n 次失败后等待 base * 2^(n-1) 秒，封顶 RetryMaxSeconds。
internal static class NotificationRetryPolicy
{
    private const int MaxExponent = 16;

    public static DateTimeOffset NextAttemptAt(NotificationDeliveryOptions options, int attempts, DateTimeOffset now)
    {
        var exponent = Math.Min(Math.Max(attempts - 1, 0), MaxExponent);
        var seconds = Math.Min((double)options.RetryBaseSeconds * Math.Pow(2, exponent), options.RetryMaxSeconds);

        return now.AddSeconds(seconds);
    }

    public static bool IsExhausted(NotificationDeliveryOptions options, int attempts) => attempts >= options.MaxAttempts;
}
