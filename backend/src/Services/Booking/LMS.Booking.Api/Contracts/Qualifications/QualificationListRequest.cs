using System.ComponentModel;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Booking.Application.Qualifications.Models;
using LMS.Booking.Domain.Qualifications;

namespace LMS.Booking.Api.Contracts.Qualifications;

public sealed class QualificationListRequest : PagedQueryRequest
{
    private static readonly ListQuerySpec Spec = new(QualificationSorting.Fields, QualificationSorting.DefaultSort, QualificationSorting.StableField);

    [Description("按物业筛选；传入时不能为空白。")]
    public string? PropertyId { get; set; }

    [Description("按资格状态筛选。")]
    public QualificationStatus? Status { get; set; }

    public override ListQuerySpec GetSortSpecification() => Spec;
}
