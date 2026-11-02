using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Application.Shifts.Models;
using LMS.Dispatch.Domain.Shifts;

namespace LMS.Dispatch.Application.Shifts.Services;

// 班次卡片上的司机姓名取自当前主档。主档已不存在时姓名留空，班次本身仍可显示。
internal sealed class ShiftCardComposer(IDriverRepository drivers)
{
    public async Task<ShiftCard> ComposeAsync(DriverShift shift, CancellationToken cancellationToken)
    {
        var cards = await ComposeAsync([shift], cancellationToken);

        return cards[0];
    }

    public async Task<IReadOnlyList<ShiftCard>> ComposeAsync(IReadOnlyList<DriverShift> shifts, CancellationToken cancellationToken)
    {
        var names = await LoadNamesAsync(shifts, cancellationToken);

        return shifts
            .Select(shift => new ShiftCard(shift, names.GetValueOrDefault(shift.DriverId), ShiftBadges.For(shift)))
            .ToList();
    }

    private async Task<Dictionary<string, string>> LoadNamesAsync(IReadOnlyList<DriverShift> shifts, CancellationToken cancellationToken)
    {
        var driverIds = shifts.Select(shift => shift.DriverId).Distinct(StringComparer.Ordinal).ToList();
        var found = await drivers.FindByIdsAsync(driverIds, cancellationToken);

        return found.ToDictionary(driver => driver.DriverId, driver => driver.Name, StringComparer.Ordinal);
    }
}
