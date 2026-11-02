using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Platform.Domain.Locations;

// 地点的局部更新。Notes 允许显式 null 以清空；嵌套对象为 null 表示该对象未传。
// Region 的变更会使地点移到新区域末尾，排序位置由应用层负责，这里不处理。
public sealed record LocationChanges(
    Optional<string> Name,
    Optional<string> Address,
    CoordinatesChanges? Coordinates,
    Optional<LocationRegion> Region,
    Optional<string> CategoryCode,
    Optional<string> ContactPerson,
    Optional<string> ContactPhone,
    Optional<string> OperatingHours,
    GeofenceChanges? Geofence,
    Optional<bool> IsActive,
    Optional<string> Notes)
{
    public IReadOnlyList<string> ChangedFields() =>
        ChangedFieldNames.From(
            ("name", Name.IsSet),
            ("address", Address.IsSet),
            ("coordinates", Coordinates is not null),
            ("region", Region.IsSet),
            ("categoryCode", CategoryCode.IsSet),
            ("contactPerson", ContactPerson.IsSet),
            ("contactPhone", ContactPhone.IsSet),
            ("operatingHours", OperatingHours.IsSet),
            ("geofence", Geofence is not null),
            ("isActive", IsActive.IsSet),
            ("notes", Notes.IsSet));
}
