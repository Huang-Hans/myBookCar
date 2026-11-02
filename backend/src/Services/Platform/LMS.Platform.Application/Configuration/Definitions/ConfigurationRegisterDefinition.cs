using LMS.BuildingBlocks.Core.Wire;
using LMS.Platform.Application.Configuration.Models;

namespace LMS.Platform.Application.Configuration.Definitions;

public static class ConfigurationRegisterDefinition
{
    public static ConfigurationListDefinition<ConfigurationRegisterEntry> Create() =>
        new(
            item => item.AreaCode,
            new Dictionary<string, Func<ConfigurationRegisterEntry, IComparable?>>
            {
                ["areaCode"] = item => item.AreaCode,
                ["areaName"] = item => item.AreaName,
                ["mechanism"] = item => WireEnum.ToWire(item.Mechanism),
                ["isEditable"] = item => item.IsEditable,
                ["owner"] = item => item.Owner,
                ["versionLabel"] = item => item.VersionLabel,
                ["sourceLabel"] = item => item.SourceLabel
            },
            item => [item.AreaCode, item.AreaName, WireEnum.ToWire(item.Mechanism), item.Owner, item.VersionLabel, item.SourceLabel],
            "areaCode",
            "areaCode");
}
