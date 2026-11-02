using LMS.Platform.Application.Configuration.Models;

namespace LMS.Platform.Application.Configuration.Definitions;

public static class PropertyDefinition
{
    public static ConfigurationListDefinition<ConfiguredProperty> Create() =>
        new(
            item => item.PropertyId,
            new Dictionary<string, Func<ConfiguredProperty, IComparable?>>
            {
                ["propertyId"] = item => item.PropertyId,
                ["name"] = item => item.Name,
                ["address"] = item => item.Address,
                ["contactPerson"] = item => item.ContactPerson,
                ["contactPhone"] = item => item.ContactPhone,
                ["assignedFleetCount"] = item => item.AssignedFleetCount,
                ["operatingHours"] = item => item.OperatingHours,
                ["serviceLevel"] = item => item.ServiceLevel
            },
            item => [item.PropertyId, item.Name, item.Address, item.ContactPerson, item.ContactPhone, item.OperatingHours, item.ServiceLevel],
            "propertyId",
            "propertyId");
}
