using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Application.Shifts.Models;
using LMS.Dispatch.Application.Shifts.Ports;
using LMS.Dispatch.Domain.Drivers;
using LMS.Dispatch.Domain.Shifts;
using LMS.Dispatch.Domain.Shifts.Batches;

namespace LMS.Dispatch.Application.Shifts.Services;

internal sealed class ShiftBatchPlanner(IDriverRepository drivers, IDriverShiftRepository shifts)
{
    public async Task<OperationResult<ShiftBatchPreview>> PlanAsync(ShiftBatchRules rules, PropertyScope scope,
        ITransactionContext? context, CancellationToken cancellationToken)
    {
        var issues = ShiftBatchRuleValidation.Validate(rules);
        if (issues.Count > 0) return OperationResult<ShiftBatchPreview>.Invalid(issues);
        var found = await drivers.FindVisibleByIdsAsync(rules.DriverIds, scope, context, cancellationToken);
        issues = ValidateDrivers(rules, found);
        if (issues.Count > 0) return OperationResult<ShiftBatchPreview>.Invalid(issues);
        var occupied = await shifts.ListForDriversBetweenAsync(rules.DriverIds, rules.From, rules.To, rules.Period, context, cancellationToken);
        var dates = rules.Dates();
        var selected = occupied.Where(shift => dates.Contains(shift.BusinessDate)).ToList();
        var items = ComposeItems(rules, scope, found, selected, dates);
        return OperationResult<ShiftBatchPreview>.Succeeded(new(
            ShiftBatchFingerprint.Create(rules, scope, found, selected), found.Count, dates.Count, items));
    }

    private static IReadOnlyList<FieldIssue> ValidateDrivers(ShiftBatchRules rules, IReadOnlyList<Driver> found)
    {
        var byId = found.ToDictionary(driver => driver.DriverId, StringComparer.Ordinal);
        return rules.DriverIds.Select((id, index) => DriverIssue(id, index, rules.PropertyId, byId))
            .OfType<FieldIssue>().ToList();
    }

    private static FieldIssue? DriverIssue(string id, int index, string? propertyId, IReadOnlyDictionary<string, Driver> found)
    {
        if (!found.TryGetValue(id, out var driver))
            return new($"driverIds[{index}]", "The driver was not found or is not visible");
        return EligibilityIssue(driver, index, propertyId);
    }

    private static FieldIssue? EligibilityIssue(Driver driver, int index, string? propertyId)
    {
        if (driver.Status != DriverStatus.Active)
            return new($"driverIds[{index}]", "Must be an active driver");
        return propertyId is null || driver.PropertyId == propertyId ? null
            : new($"driverIds[{index}]", "Driver must belong to the selected property");
    }

    private static IReadOnlyList<ShiftBatchItem> ComposeItems(ShiftBatchRules rules, PropertyScope scope,
        IReadOnlyList<Driver> found, IReadOnlyList<DriverShift> occupied, IReadOnlyList<DateOnly> dates)
    {
        var byDriverPeriod = occupied.ToDictionary(shift => (shift.DriverId, shift.BusinessDate, shift.Period));
        var ordered = found.OrderBy(driver => driver.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(driver => driver.DriverId, StringComparer.Ordinal).ToList();
        return dates.SelectMany(date => ordered.Select(driver => ComposeItem(rules, scope, driver, date,
            byDriverPeriod.GetValueOrDefault((driver.DriverId, date, rules.Period))))).ToList();
    }

    private static ShiftBatchItem ComposeItem(ShiftBatchRules rules, PropertyScope scope, Driver driver,
        DateOnly date, DriverShift? existing)
    {
        if (existing is null)
            return new(date, driver.DriverId, driver.Name, false, rules.Period, rules.DutyStatus, rules.Role, null, driver.PropertyId);
        if (!scope.Allows(existing.PropertyId))
            return new(date, driver.DriverId, driver.Name, true, null, null, null, null, null);
        return new(date, driver.DriverId, driver.Name, true, existing.Period, existing.DutyStatus,
            existing.Role, existing.ShiftId, existing.PropertyId);
    }
}
