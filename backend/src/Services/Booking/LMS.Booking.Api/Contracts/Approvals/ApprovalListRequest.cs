using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Booking.Application.Approvals.Models;
using LMS.Booking.Domain.Approvals;

namespace LMS.Booking.Api.Contracts.Approvals;

public sealed class ApprovalListRequest : PagedQueryRequest, IValidatableObject
{
    private static readonly ListQuerySpec Spec = new(ApprovalSorting.Fields, ApprovalSorting.DefaultSort, ApprovalSorting.StableField);

    [Description("按物业筛选；传入时不能为空白。")]
    public string? PropertyId { get; set; }

    [Description("按审批状态筛选。")]
    public ApprovalStatus? Status { get; set; }

    [Description("起始时间（含），ISO 8601 UTC 时间并以 Z 结尾。")]
    public DateTimeOffset? From { get; set; }

    [Description("结束时间（不含），ISO 8601 UTC 时间并以 Z 结尾；与 from 同时给出时必须晚于 from。")]
    public DateTimeOffset? To { get; set; }

    public override ListQuerySpec GetSortSpecification() => Spec;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => ApprovalTimeRange.Validate(From, To);
}
