using System.ComponentModel;
using LMS.BuildingBlocks.Http.Requests.Contracts;

namespace LMS.Booking.Api.Contracts.Approvals;

public sealed class BlacklistHitListRequest : QueryRequest
{
    [Description("按物业筛选；传入时不能为空白。")]
    public string? PropertyId { get; set; }
}
