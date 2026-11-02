using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LMS.BuildingBlocks.Http.Requests.Contracts;

namespace LMS.Booking.Api.Contracts.Customer;

public sealed class RideOptionsRequest : QueryRequest
{
    [Required]
    [Description("服务产品代码：BUSINESS_SEDAN / LIMOUSINE / PARTY / CROSS_BORDER_TRANSFER。")]
    public string? ProductCode { get; set; }

    [Required]
    [Description("上车地点标识（Platform 目录地点）。")]
    public string? PickupLocationId { get; set; }

    [Required]
    [Description("下车地点标识（Platform 目录地点）。")]
    public string? DropoffLocationId { get; set; }

    [Range(1, 50, ErrorMessage = "Must be between 1 and 50")]
    [Description("乘客数，用于判断每个车型是否容纳；不传时按 1。")]
    public int Passengers { get; set; } = 1;
}
