using LMS.Booking.Application.SelfService.Ports;

namespace LMS.Booking.Application.SelfService.Models;

// 客户首页的服务目录：城市、产品及可订状态，以及 Platform 自助预约规则中对选择时间有影响的部分。
public sealed record ServiceOfferings(
    IReadOnlyList<CityOffering> Cities,
    IReadOnlyList<ProductOffering> Products,
    SelfBookingRules Rules,
    bool IsAsapAvailable);
