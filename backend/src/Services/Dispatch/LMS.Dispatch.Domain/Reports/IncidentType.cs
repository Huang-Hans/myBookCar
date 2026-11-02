namespace LMS.Dispatch.Domain.Reports;

// 事故 / 事件类型（对应原型四项）。对外字面值为 VEHICLE_BREAKDOWN / TRAFFIC_ACCIDENT / PASSENGER_ASSISTANCE / OTHER。
public enum IncidentType
{
    VehicleBreakdown,
    TrafficAccident,
    PassengerAssistance,
    Other
}
