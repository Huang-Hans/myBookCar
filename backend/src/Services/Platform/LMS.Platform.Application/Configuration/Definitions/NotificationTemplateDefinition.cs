using LMS.BuildingBlocks.Core.Wire;
using LMS.Platform.Application.Configuration.Models;

namespace LMS.Platform.Application.Configuration.Definitions;

public static class NotificationTemplateDefinition
{
    public static ConfigurationListDefinition<NotificationTemplate> Create() =>
        new(
            item => item.TemplateId,
            new Dictionary<string, Func<NotificationTemplate, IComparable?>>
            {
                ["templateId"] = item => item.TemplateId,
                ["eventType"] = item => WireEnum.ToWire(item.EventType),
                ["eventTrigger"] = item => item.EventTrigger,
                ["isActive"] = item => item.IsActive
            },
            SearchTexts,
            "templateId",
            "templateId");

    private static IEnumerable<string?> SearchTexts(NotificationTemplate item) =>
        new[] { item.TemplateId, WireEnum.ToWire(item.EventType), item.EventTrigger }
            .Concat(item.Channels.Select(channel => WireEnum.ToWire(channel)));
}
