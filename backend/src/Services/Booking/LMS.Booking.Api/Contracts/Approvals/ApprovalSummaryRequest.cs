using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Booking.Domain.Approvals;

namespace LMS.Booking.Api.Contracts.Approvals;

public sealed class ApprovalSummaryRequest : QueryRequest, IValidatableObject
{
    [UnrestrictedText]
    [Description("在可见字段中进行不区分大小写的搜索；空白表示不筛选。")]
    public string Search { get; set; } = string.Empty;

    [Description("按物业筛选；传入时不能为空白。")]
    public string? PropertyId { get; set; }

    [Description("按审批状态筛选。")]
    public ApprovalStatus? Status { get; set; }

    [Description("起始时间（含），ISO 8601 UTC 时间并以 Z 结尾。")]
    public DateTimeOffset? From { get; set; }

    [Description("结束时间（不含），ISO 8601 UTC 时间并以 Z 结尾；与 from 同时给出时必须晚于 from。")]
    public DateTimeOffset? To { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => ApprovalTimeRange.Validate(From, To);
}
