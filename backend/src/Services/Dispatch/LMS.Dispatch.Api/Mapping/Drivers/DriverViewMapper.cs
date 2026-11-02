using System.Globalization;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Dispatch.Api.Contracts.Drivers;
using LMS.Dispatch.Domain.Drivers;

namespace LMS.Dispatch.Api.Mapping.Drivers;

internal static class DriverViewMapper
{
    private const string DateFormat = "yyyy-MM-dd";

    public static DriverView ToView(Driver driver) => new(
        driver.DriverId,
        driver.Name,
        driver.LicenseNumber,
        FormatDate(driver.LicenseExpiryDate),
        driver.Phone,
        driver.PropertyId,
        driver.AssignedVehicle,
        WireEnum.ToWire(driver.Status),
        driver.Languages,
        new EmergencyContactView(driver.EmergencyContact.Name, driver.EmergencyContact.Phone),
        driver.Availability is null ? null : WireEnum.ToWire(driver.Availability.Value),
        driver.AvailabilitySource,
        driver.SyncedAt?.UtcDateTime,
        driver.Restriction is null
            ? null
            : new DriverRestrictionView(driver.Restriction.Reason, driver.Restriction.EffectiveDate is null ? null : FormatDate(driver.Restriction.EffectiveDate.Value)),
        driver.Version) { AssignedVehicleId = driver.AssignedVehicleId };

    private static string FormatDate(DateOnly date) => date.ToString(DateFormat, CultureInfo.InvariantCulture);
}
