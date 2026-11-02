namespace LMS.Dispatch.Infrastructure.Persistence;

// lms_dispatch 库中的集合名。
internal static class DispatchCollections
{
    public const string Drivers = "drivers";
    public const string DriverOffers = "driver_offers";
    public const string DriverInspections = "driver_inspections";
    public const string VehicleIssues = "vehicle_issues";
    public const string DriverIncidents = "driver_incidents";
    public const string DriverPreferences = "driver_preferences";
    public const string DriverShifts = "driver_shifts";
    public const string ShiftBatches = "shift_batches";
    public const string Vehicles = "vehicles";
    public const string Coupons = "coupons";
    public const string CouponRedemptions = "coupon_redemptions";
    public const string AuditRecords = "audit_records";
    public const string IdempotencyRecords = "idempotency_records";
}
