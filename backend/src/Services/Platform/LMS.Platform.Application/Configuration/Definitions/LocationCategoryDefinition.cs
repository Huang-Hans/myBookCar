using LMS.Platform.Application.Configuration.Models;

namespace LMS.Platform.Application.Configuration.Definitions;

public static class LocationCategoryDefinition
{
    public static ConfigurationListDefinition<LocationCategory> Create() =>
        new(
            item => item.CategoryCode,
            new Dictionary<string, Func<LocationCategory, IComparable?>>
            {
                ["categoryCode"] = item => item.CategoryCode,
                ["name"] = item => item.Name,
                ["description"] = item => item.Description,
                ["isActive"] = item => item.IsActive
            },
            item => [item.CategoryCode, item.Name, item.Description],
            "categoryCode",
            "categoryCode");
}
