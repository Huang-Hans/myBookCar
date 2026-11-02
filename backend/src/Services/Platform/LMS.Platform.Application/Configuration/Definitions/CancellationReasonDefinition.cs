using LMS.Platform.Application.Configuration.Models;

namespace LMS.Platform.Application.Configuration.Definitions;

public static class CancellationReasonDefinition
{
    public static ConfigurationListDefinition<CancellationReason> Create() =>
        new(
            item => item.ReasonCode,
            new Dictionary<string, Func<CancellationReason, IComparable?>>
            {
                ["reasonCode"] = item => item.ReasonCode,
                ["name"] = item => item.Name,
                ["pointsReturnPolicyCode"] = item => item.PointsReturnPolicyCode,
                ["pointsReturnReference"] = item => item.PointsReturnReference,
                ["source"] = item => item.Source
            },
            item => [item.ReasonCode, item.Name, item.PointsReturnPolicyCode, item.PointsReturnReference, item.Source],
            "reasonCode",
            "reasonCode");
}
