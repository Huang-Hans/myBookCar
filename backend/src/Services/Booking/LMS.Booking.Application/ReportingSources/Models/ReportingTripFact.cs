using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.ReportingSources.Models;

// 报表来源的一条行程事实：行程本身，加上由行驶轨迹算出的两段距离（没有足够轨迹点时为 null）。
// PickupToCompleteMeters 是接到客人到完成的载客里程；EnRouteToCompleteMeters 是出发到完成的车辆里程。
public sealed record ReportingTripFact(Trip Trip, double? PickupToCompleteMeters, double? EnRouteToCompleteMeters);
