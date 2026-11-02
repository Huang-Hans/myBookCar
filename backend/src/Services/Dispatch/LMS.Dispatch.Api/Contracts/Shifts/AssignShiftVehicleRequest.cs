namespace LMS.Dispatch.Api.Contracts.Shifts;

/// <summary>
/// Legacy adapter for changing the driver's vehicle binding. New clients patch assignedVehicleId on the driver.
/// </summary>
/// <param name="VehiclePlate">Legacy lookup input only. Resolves the vehicle ID and updates the driver's binding for all shifts; never stored on a shift.</param>
public sealed record AssignShiftVehicleRequest(string? VehiclePlate);
