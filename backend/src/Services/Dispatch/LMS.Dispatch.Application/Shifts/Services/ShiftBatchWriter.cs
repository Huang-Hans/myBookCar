using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Auditing.Ports;
using LMS.Dispatch.Application.Auditing.Services;
using LMS.Dispatch.Application.Idempotency.Models;
using LMS.Dispatch.Application.Idempotency.Options;
using LMS.Dispatch.Application.Idempotency.Ports;
using LMS.Dispatch.Application.Shifts.Commands;
using LMS.Dispatch.Application.Shifts.Models;
using LMS.Dispatch.Application.Shifts.Ports;
using LMS.Dispatch.Domain.Shifts;
using LMS.Dispatch.Domain.Shifts.Batches;
using Microsoft.Extensions.Options;

namespace LMS.Dispatch.Application.Shifts.Services;

internal sealed class ShiftBatchWriter(
    IDriverShiftRepository shifts, IShiftBatchRepository batches, IAuditRecordRepository audits,
    IIdempotencyRecordRepository records, IOptions<IdempotencyOptions> options, TimeProvider timeProvider)
{
    public async Task<ShiftBatch> WriteAsync(CreateShiftBatchCommand command, ShiftBatchPreview preview,
        IdempotencyFingerprint fingerprint, ITransactionContext context, CancellationToken cancellationToken)
    {
        var created = CreateShifts(command.Rules, preview);
        var now = timeProvider.GetUtcNow();
        var batch = BuildBatch(command, preview, created, now);
        await shifts.InsertManyAsync(created, context, cancellationToken);
        var auditRecords = created.Select(shift => AuditRecordFactory.Created(command.Context,
            "shift.batch-create", "shift", shift.ShiftId, now, shift.PropertyId)).ToList();
        await audits.InsertManyAsync(auditRecords, context, cancellationToken);
        await batches.InsertAsync(batch, context, cancellationToken);
        await records.InsertAsync(new(fingerprint, batch.BatchId, now, now.AddHours(options.Value.RetentionHours)), context, cancellationToken);
        return batch;
    }

    private static IReadOnlyList<DriverShift> CreateShifts(ShiftBatchRules rules, ShiftBatchPreview preview) =>
        preview.Items.Where(item => !item.IsExisting)
            .Select(item => DriverShift.Create(item.BusinessDate, rules.Period, item.DriverId,
                rules.DutyStatus, rules.Role) with { PropertyId = item.PropertyId ?? string.Empty }).ToList();

    private static ShiftBatch BuildBatch(CreateShiftBatchCommand command, ShiftBatchPreview preview,
        IReadOnlyList<DriverShift> created, DateTimeOffset now)
    {
        var ids = created.ToDictionary(shift => (shift.DriverId, shift.BusinessDate), shift => shift.ShiftId);
        var items = preview.Items.Select(item => item.IsExisting ? item
            : item with { ShiftId = ids[(item.DriverId, item.BusinessDate)] }).ToList();
        return new("shiftbatch_" + Guid.CreateVersion7().ToString("N"), command.Context.Actor,
            now, items);
    }
}
