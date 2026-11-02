using LMS.Platform.Application.Configuration.Models;

namespace LMS.Platform.Application.Configuration.Definitions;

public static class IntegrationReferenceDefinition
{
    public static ConfigurationListDefinition<IntegrationReference> Create() =>
        new(
            item => item.IntegrationCode,
            new Dictionary<string, Func<IntegrationReference, IComparable?>> { ["integrationCode"] = item => item.IntegrationCode },
            item => [item.IntegrationCode, item.Name],
            "integrationCode",
            "integrationCode");
}
