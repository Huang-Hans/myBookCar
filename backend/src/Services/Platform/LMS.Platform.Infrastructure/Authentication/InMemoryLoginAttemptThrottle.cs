using System.Collections.Concurrent;
using LMS.Platform.Application.Authentication.Options;
using LMS.Platform.Application.Authentication.Ports;
using Microsoft.Extensions.Options;

namespace LMS.Platform.Infrastructure.Authentication;

// 进程内的登录失败计数：窗口内连续失败达到上限后拒绝，成功登录清零。多实例部署时每个实例各自计数，
// 因此是尽力而为的口令猜测缓解，不是全局限流；不依赖新增中间件或外部存储。
internal sealed class InMemoryLoginAttemptThrottle(IOptions<AppAuthenticationOptions> options, TimeProvider timeProvider)
    : ILoginAttemptThrottle
{
    private const int PruneThreshold = 10_000;

    private readonly ConcurrentDictionary<string, FailureWindow> windows = new(StringComparer.Ordinal);

    public bool IsBlocked(string key) =>
        windows.TryGetValue(key, out var window)
        && window.StartedAt + Window > timeProvider.GetUtcNow()
        && window.Count >= options.Value.MaxFailedLogins;

    public void RecordFailure(string key)
    {
        var now = timeProvider.GetUtcNow();
        windows.AddOrUpdate(
            key,
            _ => new FailureWindow(now, 1),
            (_, existing) => existing.StartedAt + Window > now ? existing with { Count = existing.Count + 1 } : new FailureWindow(now, 1));
        if (windows.Count > PruneThreshold)
        {
            Prune(now);
        }
    }

    public void Reset(string key) => windows.TryRemove(key, out _);

    private TimeSpan Window => TimeSpan.FromMinutes(options.Value.FailureWindowMinutes);

    private void Prune(DateTimeOffset now)
    {
        foreach (var pair in windows.Where(pair => pair.Value.StartedAt + Window <= now))
        {
            windows.TryRemove(pair.Key, out _);
        }
    }

    private sealed record FailureWindow(DateTimeOffset StartedAt, int Count);
}
