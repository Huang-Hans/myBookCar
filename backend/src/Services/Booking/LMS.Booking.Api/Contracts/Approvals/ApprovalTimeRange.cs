using System.ComponentModel.DataAnnotations;

namespace LMS.Booking.Api.Contracts.Approvals;

// 审批列表与汇总共用的跨参数校验：from 与 to 同时给出时，to 必须晚于 from。
internal static class ApprovalTimeRange
{
    public static IEnumerable<ValidationResult> Validate(DateTimeOffset? from, DateTimeOffset? to)
    {
        if (from is not null && to is not null && from >= to)
        {
            yield return new ValidationResult("Must be later than from", [nameof(ApprovalListRequest.To)]);
        }
    }
}
