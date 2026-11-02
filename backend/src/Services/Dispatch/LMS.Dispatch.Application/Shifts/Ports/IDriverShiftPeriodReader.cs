using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Domain.Shifts;

namespace LMS.Dispatch.Application.Shifts.Ports;

public interface IDriverShiftPeriodReader
{
    Task<IReadOnlyList<DriverShift>> ListOnDutyAsync(DateOnly businessDate, ShiftPeriod period,
        PropertyScope scope, string? propertyId, CancellationToken cancellationToken);
}
