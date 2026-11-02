using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Domain.Shifts;
using LMS.Dispatch.Infrastructure.Documents;
using LMS.Dispatch.Infrastructure.Persistence;
using LMS.Dispatch.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Dispatch.Infrastructure.Repositories;

internal sealed partial class MongoDriverShiftRepository
{
    public async Task<bool> SupportsWithdrawalAsync(CancellationToken cancellationToken)
    {
        using var cursor = await Collection.Indexes.ListAsync(cancellationToken: cancellationToken);
        var indexes = await cursor.ToListAsync(cancellationToken);
        var names = indexes.Select(index => index["name"].AsString).ToHashSet(StringComparer.Ordinal);
        return names.Contains("ux_active_driver_date_period")
            && !names.Contains("ux_driver_date_period") && !names.Contains("ux_driver_date");
    }

    public async Task<IReadOnlyList<DriverShift>> ListWithdrawalTargetsAsync(IReadOnlyList<string> shiftIds,
        PropertyScope scope, ITransactionContext context, CancellationToken cancellationToken)
    {
        var filter = Builders<DriverShiftDocument>.Filter;
        return await ReadAsync(filter.In(shift => shift.ShiftId, shiftIds), scope, null, context, cancellationToken);
    }

    public async Task<IReadOnlyList<DriverShift>> ListActivePeriodAsync(DateOnly date, ShiftPeriod period, PropertyScope scope,
        string? propertyId, ITransactionContext context, CancellationToken cancellationToken)
    {
        var filter = Builders<DriverShiftDocument>.Filter;
        var matches = filter.And(Active, filter.Eq(shift => shift.BusinessDate, StoredValues.FromDate(date)),
            filter.Eq(shift => shift.Period, WireEnum.ToWire(period)));
        return await ReadAsync(matches, scope, propertyId, context, cancellationToken);
    }

    public async Task<bool> TryWithdrawManyAsync(IReadOnlyList<DriverShift> shifts, string actor, DateTimeOffset now,
        ITransactionContext context, CancellationToken cancellationToken)
    {
        var writes = shifts.Select(shift => Withdrawal(shift, actor, now)).ToList();
        var result = await Collection.BulkWriteAsync(MongoTransactionContext.RequireSession(context),
            writes, new BulkWriteOptions { IsOrdered = true }, cancellationToken);
        return result.MatchedCount == shifts.Count;
    }

    private static WriteModel<DriverShiftDocument> Withdrawal(DriverShift shift, string actor, DateTimeOffset now)
    {
        var filter = Builders<DriverShiftDocument>.Filter;
        var matches = filter.And(Active, filter.Eq(item => item.ShiftId, shift.ShiftId),
            filter.Eq(item => item.Version, shift.Version));
        var updated = shift with { WithdrawnAt = now, WithdrawnBy = actor, Version = shift.Version + 1 };
        return new ReplaceOneModel<DriverShiftDocument>(matches, DriverShiftDocument.FromDomain(updated));
    }
}
