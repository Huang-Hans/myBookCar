using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Shifts.Models;
using LMS.Dispatch.Domain.Drivers;
using LMS.Dispatch.Domain.Shifts;

namespace LMS.Dispatch.Application.Shifts.Services;

internal static class ShiftBatchFingerprint
{
    public static string Create(ShiftBatchRules rules, PropertyScope scope,
        IReadOnlyList<Driver> drivers, IReadOnlyList<DriverShift> shifts)
    {
        var snapshot = JsonSerializer.Serialize(new
        {
            Rules = rules.ToCanonicalForm(),
            Scope = scope,
            Drivers = drivers.OrderBy(driver => driver.DriverId, StringComparer.Ordinal)
                .Select(driver => new { driver.DriverId, driver.Name, driver.Status, driver.PropertyId }),
            Shifts = shifts.OrderBy(shift => shift.ShiftId, StringComparer.Ordinal)
                .Select(shift => new { shift.ShiftId, shift.DriverId, shift.BusinessDate, shift.Period,
                    shift.DutyStatus, shift.Role, shift.Attendance, shift.Version, shift.PropertyId, shift.WithdrawnAt })
        });
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot)));
    }
}
