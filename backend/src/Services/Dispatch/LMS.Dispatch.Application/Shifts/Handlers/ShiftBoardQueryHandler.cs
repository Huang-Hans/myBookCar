using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Drivers.Options;
using LMS.Dispatch.Application.Drivers.Services;
using LMS.Dispatch.Application.Shifts.Models;
using LMS.Dispatch.Application.Shifts.Ports;
using LMS.Dispatch.Application.Shifts.Services;
using LMS.Dispatch.Domain.Shifts;
using Microsoft.Extensions.Options;

namespace LMS.Dispatch.Application.Shifts.Handlers;

// 班次看板按一个业务日期返回早、午、夜三段。没有班次的时段也返回，计数为 0。
// 未来列表从当天起连续 15 个业务日期，没有班次的日期也返回。
internal sealed class ShiftBoardQueryHandler(
    IDriverShiftRepository shifts,
    ShiftCardComposer cards,
    BusinessCalendar calendar,
    PickupShiftQuery pickupShifts,
    IOptions<DispatchBusinessOptions> options) : IShiftBoardQueryHandler
{
    private const int UpcomingDays = 15;
    private const int MaxCalendarDays = 31;

    public async Task<OperationResult<ShiftBoard>> GetBoardAsync(ShiftBoardQuery query, CancellationToken cancellationToken)
    {
        var property = ReadProperty(query.PropertyId, query.Scope);
        if (!property.IsSuccess)
        {
            return OperationResult<ShiftBoard>.FailureOf(property);
        }

        if (query.PickupAt is { } pickupAt)
            return await GetPickupBoardAsync(pickupAt, query.Scope, property.Value, cancellationToken);

        var propertyId = property.Value;
        var businessDate = query.BusinessDate ?? calendar.Today();
        var found = await shifts.ListForBoardAsync(businessDate, query.Scope, propertyId, cancellationToken);
        var composed = await cards.ComposeAsync(found, cancellationToken);
        return OperationResult<ShiftBoard>.Succeeded(Board(businessDate, propertyId, composed));
    }

    private async Task<OperationResult<ShiftBoard>> GetPickupBoardAsync(DateTimeOffset pickupAt,
        PropertyScope scope, string? propertyId, CancellationToken cancellationToken)
    {
        var slot = pickupShifts.Resolve(pickupAt);
        var found = await pickupShifts.ListAsync(slot, scope, propertyId, cancellationToken);
        var composed = await cards.ComposeAsync(found, cancellationToken);
        var board = new ShiftBoard(options.Value.TimeZoneId, slot.BusinessDate, propertyId, [Column(slot.Period, composed)]);
        return OperationResult<ShiftBoard>.Succeeded(board);
    }

    public async Task<OperationResult<IReadOnlyList<ShiftBoard>>> GetCalendarAsync(ShiftCalendarQuery query, CancellationToken cancellationToken)
    {
        var dayCount = query.To.DayNumber - query.From.DayNumber + 1;
        if (!IsSupportedCalendarLength(dayCount))
        {
            return OperationResult<IReadOnlyList<ShiftBoard>>.Invalid(
                [new FieldIssue("to", $"Must not be earlier than from and span at most {MaxCalendarDays} days")]);
        }

        var property = ReadProperty(query.PropertyId, query.Scope);
        if (!property.IsSuccess)
        {
            return OperationResult<IReadOnlyList<ShiftBoard>>.FailureOf(property);
        }

        var found = await shifts.ListBetweenAsync(query.From, query.To, query.Scope, property.Value, cancellationToken);
        var composed = await cards.ComposeAsync(found, cancellationToken);
        var byDate = composed.ToLookup(card => card.Shift.BusinessDate);
        var boards = Enumerable.Range(0, dayCount)
            .Select(offset => query.From.AddDays(offset))
            .Select(date => Board(date, property.Value, byDate[date].ToList()))
            .ToList();

        return OperationResult<IReadOnlyList<ShiftBoard>>.Succeeded(boards);
    }

    public async Task<OperationResult<ShiftUpcoming>> GetUpcomingAsync(ShiftBoardQuery query, CancellationToken cancellationToken)
    {
        var property = ReadProperty(query.PropertyId, query.Scope);
        if (!property.IsSuccess)
        {
            return OperationResult<ShiftUpcoming>.FailureOf(property);
        }

        var from = calendar.Today();
        var to = from.AddDays(UpcomingDays - 1);
        var found = await shifts.ListBetweenAsync(from, to, query.Scope, property.Value, cancellationToken);
        var composed = await cards.ComposeAsync(found, cancellationToken);
        var days = Enumerable.Range(0, UpcomingDays)
            .Select(offset => Day(from.AddDays(offset), composed))
            .ToList();

        return OperationResult<ShiftUpcoming>.Succeeded(new ShiftUpcoming(options.Value.TimeZoneId, from, to, property.Value, days));
    }

    public async Task<OperationResult<ShiftCard>> GetAsync(string shiftId, PropertyScope scope, CancellationToken cancellationToken)
    {
        var shift = await shifts.FindVisibleAsync(shiftId, scope, cancellationToken);

        return shift is null
            ? OperationResult<ShiftCard>.Failed(OperationOutcome.NotFound)
            : OperationResult<ShiftCard>.Succeeded(await cards.ComposeAsync(shift, cancellationToken));
    }

    private static OperationResult<string?> ReadProperty(string? requestedPropertyId, PropertyScope scope)
    {
        if (requestedPropertyId is not null && string.IsNullOrWhiteSpace(requestedPropertyId))
        {
            return OperationResult<string?>.Invalid([new FieldIssue("propertyId", "Must not be blank")]);
        }

        var propertyId = requestedPropertyId?.Trim();
        if (propertyId is not null && !scope.Allows(propertyId))
        {
            return OperationResult<string?>.Failed(OperationOutcome.Forbidden);
        }

        return OperationResult<string?>.Succeeded(propertyId);
    }

    private static bool IsSupportedCalendarLength(int dayCount) => dayCount >= 1 && dayCount <= MaxCalendarDays;

    private ShiftBoard Board(DateOnly businessDate, string? propertyId, IReadOnlyList<ShiftCard> cards) =>
        new(options.Value.TimeZoneId, businessDate, propertyId,
            Enum.GetValues<ShiftPeriod>().Select(period => Column(period, cards)).ToList());

    private static ShiftDay Day(DateOnly businessDate, IReadOnlyList<ShiftCard> cards) =>
        new(businessDate, Order(cards.Where(card => card.Shift.BusinessDate == businessDate)));

    // 首选取在待命之前，同一角色内按司机姓名排序，方便看板与页面卡片顺序一致。
    private static ShiftColumn Column(ShiftPeriod period, IReadOnlyList<ShiftCard> cards) =>
        new(period, ShiftWindows.Of(period), Order(cards.Where(card => card.Shift.Period == period)));

    private static IReadOnlyList<ShiftCard> Order(IEnumerable<ShiftCard> cards) =>
        cards
            .OrderBy(card => card.Shift.Period)
            .ThenBy(card => card.Shift.Role == ShiftRole.Preferred ? 0 : 1)
            .ThenBy(card => card.DriverName ?? card.Shift.DriverId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(card => card.Shift.ShiftId, StringComparer.Ordinal)
            .ToList();
}
