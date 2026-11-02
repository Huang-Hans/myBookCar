namespace LMS.Platform.Domain.Locations;

// 常用地点主档。SortOrder 是区域内的持久排序位置（从 1 开始），不属于普通编辑字段。
public sealed record CommonLocation(
    string LocationId,
    string Name,
    string Address,
    Coordinates Coordinates,
    LocationRegion Region,
    string CategoryCode,
    string ContactPerson,
    string ContactPhone,
    string OperatingHours,
    Geofence Geofence,
    bool IsActive,
    string? Notes,
    int SortOrder,
    long Version)
{
    private const string IdPrefix = "location_";
    private const long FirstVersion = 1;

    public static CommonLocation Create(
        string name,
        string address,
        Coordinates coordinates,
        LocationRegion region,
        string categoryCode,
        string contactPerson,
        string contactPhone,
        string operatingHours,
        Geofence geofence,
        bool isActive,
        string? notes,
        int sortOrder) =>
        new(
            IdPrefix + Guid.CreateVersion7().ToString("N"),
            name, address, coordinates, region, categoryCode, contactPerson, contactPhone,
            operatingHours, geofence, isActive, notes, sortOrder, FirstVersion);

    public CommonLocation Apply(LocationChanges changes) =>
        this with
        {
            Name = changes.Name.ValueOr(Name),
            Address = changes.Address.ValueOr(Address),
            Coordinates = changes.Coordinates?.ApplyTo(Coordinates) ?? Coordinates,
            Region = changes.Region.ValueOr(Region),
            CategoryCode = changes.CategoryCode.ValueOr(CategoryCode),
            ContactPerson = changes.ContactPerson.ValueOr(ContactPerson),
            ContactPhone = changes.ContactPhone.ValueOr(ContactPhone),
            OperatingHours = changes.OperatingHours.ValueOr(OperatingHours),
            Geofence = changes.Geofence?.ApplyTo(Geofence) ?? Geofence,
            IsActive = changes.IsActive.ValueOr(IsActive),
            Notes = changes.Notes.ValueOrCurrent(Notes)
        };
}
