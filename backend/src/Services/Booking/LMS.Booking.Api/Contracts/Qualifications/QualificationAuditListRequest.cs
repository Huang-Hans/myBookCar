using System.ComponentModel;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Booking.Application.Qualifications.Models;

namespace LMS.Booking.Api.Contracts.Qualifications;

public sealed class QualificationAuditListRequest : PagedQueryRequest
{
    private static readonly ListQuerySpec Spec =
        new(QualificationAuditSorting.Fields, QualificationAuditSorting.DefaultSort, QualificationAuditSorting.StableField);

    [Description("按物业筛选；传入时不能为空白。")]
    public string? PropertyId { get; set; }

    public override ListQuerySpec GetSortSpecification() => Spec;
}
