using System.Globalization;
using System.Text.Json;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Domain.Shifts;

namespace LMS.Dispatch.Application.Shifts.Commands;

public sealed record CreateShiftCommand(
    DateOnly BusinessDate,
    ShiftPeriod Period,
    string DriverId,
    ShiftDutyStatus DutyStatus,
    ShiftRole Role,
    string IdempotencyKey,
    CommandContext Context)
{
    public string ToCanonicalForm() =>
        JsonSerializer.Serialize(new object?[]
        {
            BusinessDate.ToString("O", CultureInfo.InvariantCulture),
            WireEnum.ToWire(Period),
            DriverId,
            WireEnum.ToWire(DutyStatus),
            WireEnum.ToWire(Role)
        });
}
