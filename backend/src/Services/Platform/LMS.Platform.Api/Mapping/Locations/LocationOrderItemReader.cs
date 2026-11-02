using LMS.BuildingBlocks.Http.Requests;
using LMS.Platform.Api.Contracts.Locations;
using LMS.Platform.Application.Locations.Models;

namespace LMS.Platform.Api.Mapping.Locations;

internal static class LocationOrderItemReader
{
    public static IReadOnlyList<LocationOrderItem> Read(FieldErrorCollector errors, List<LocationOrderItemRequest?>? requested)
    {
        if (requested is null || requested.Count == 0)
        {
            errors.Add("items", "Must contain at least one location");
            return [];
        }

        var items = requested.Select((item, index) => ReadItem(errors, item, index)).ToList();
        if (items.GroupBy(item => item.LocationId, StringComparer.Ordinal).Any(group => group.Count() > 1))
        {
            errors.Add("items", "Each location must appear exactly once");
        }

        return items;
    }

    private static LocationOrderItem ReadItem(FieldErrorCollector errors, LocationOrderItemRequest? item, int index)
    {
        if (item is null)
        {
            errors.Add($"items[{index}]", "Must not be null");
            return new LocationOrderItem(string.Empty, 0);
        }

        var locationId = errors.RequiredText($"items[{index}].locationId", item.LocationId);
        // 乐观并发暂时取消：条目里的版本可省略，写入时以服务端读到的当前版本为准。
        var version = item.ExpectedVersion ?? 0;

        return new LocationOrderItem(locationId, version);
    }
}
