using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LMS.BuildingBlocks.Http.Requests.Contracts;

namespace LMS.Booking.Api.Contracts.TripLocations;

public sealed class TripTrackRequest : QueryRequest
{
    [Range(typeof(long), "0", "9223372036854775807", ErrorMessage = "Must be greater than or equal to 0")]
    [Description("只返回序号大于该值的点，用于增量补拉；不传时返回全部。")]
    public long AfterSeq { get; set; }
}
