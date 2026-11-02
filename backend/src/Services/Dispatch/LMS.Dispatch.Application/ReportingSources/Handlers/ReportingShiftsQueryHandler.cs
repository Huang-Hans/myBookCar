using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Application.ReportingSources.Models;
using LMS.Dispatch.Application.Shifts.Ports;

namespace LMS.Dispatch.Application.ReportingSources.Handlers;

internal sealed class ReportingShiftsQueryHandler(
    IDriverShiftRepository shifts,
    IDriverRepository drivers) : IReportingShiftsQueryHandler
{
    public async Task<IReadOnlyList<ReportingShift>> ListAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var found = await shifts.ListBetweenAsync(from, to, PropertyScope.AllProperties, null, cancellationToken);
        var driverIds = found.Select(shift => shift.DriverId).Distinct(StringComparer.Ordinal).ToList();
        var names = (await drivers.FindByIdsAsync(driverIds, cancellationToken))
            .ToDictionary(driver => driver.DriverId, driver => driver.Name, StringComparer.Ordinal);

        return found.Select(shift => new ReportingShift(shift, names.GetValueOrDefault(shift.DriverId))).ToList();
    }
}
