using LMS.Platform.Application.Configuration.Models;
using LMS.Platform.Infrastructure.Persistence;

namespace LMS.Platform.Infrastructure.Configuration.Documents;

internal static class NotificationChannelParser
{
    // 渠道必须非空且不重复；违反约束的正文视为损坏。
    public static IReadOnlyList<NotificationChannel> ParseDistinct(IReadOnlyCollection<string> channels)
    {
        var parsed = channels.Select(channel => StoredValues.ParseEnum<NotificationChannel>(channel, "channels")).ToList();
        if (parsed.Count == 0 || parsed.Distinct().Count() != parsed.Count)
        {
            throw new InvalidStoredDataException("Notification channels must be non-empty and unique.");
        }

        return parsed;
    }
}
