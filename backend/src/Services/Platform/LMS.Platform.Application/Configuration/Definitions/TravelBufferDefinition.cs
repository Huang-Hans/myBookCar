using LMS.BuildingBlocks.Core.Wire;
using LMS.Platform.Application.Configuration.Models;

namespace LMS.Platform.Application.Configuration.Definitions;

public static class TravelBufferDefinition
{
    public static ConfigurationListDefinition<TravelBuffer> Create() =>
        new(
            item => item.RouteCategoryCode,
            new Dictionary<string, Func<TravelBuffer, IComparable?>>
            {
                ["routeCategoryCode"] = item => item.RouteCategoryCode,
                ["routeCategory"] = item => item.RouteCategory,
                ["bufferMinutes"] = item => item.BufferMinutes,
                ["navigationProvider"] = item => WireEnum.ToWire(item.NavigationProvider),
                ["isTrafficAwareEtaEnabled"] = item => item.IsTrafficAwareEtaEnabled,
                ["applicationRule"] = item => item.ApplicationRule
            },
            item => [item.RouteCategoryCode, item.RouteCategory, WireEnum.ToWire(item.NavigationProvider), item.ApplicationRule],
            "routeCategoryCode",
            "routeCategoryCode");
}
