using LMS.BuildingBlocks.Core.Wire;
using LMS.Platform.Application.Configuration.Models;

namespace LMS.Platform.Application.Configuration.Definitions;

public static class NotificationChannelDefinition
{
    public static ConfigurationListDefinition<NotificationChannelConfiguration> Create() =>
        new(
            item => WireEnum.ToWire(item.EventType),
            new Dictionary<string, Func<NotificationChannelConfiguration, IComparable?>>
            {
                ["eventType"] = item => WireEnum.ToWire(item.EventType),
                ["eventTrigger"] = item => item.EventTrigger,
                ["isActive"] = item => item.IsActive
            },
            SearchTexts,
            "eventType",
            "eventType");

    private static IEnumerable<string?> SearchTexts(NotificationChannelConfiguration item) =>
        new[] { WireEnum.ToWire(item.EventType), item.EventTrigger }
            .Concat(item.Channels.Select(channel => WireEnum.ToWire(channel)));
}
