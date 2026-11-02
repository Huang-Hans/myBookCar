using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Auditing.Ports;
using LMS.Dispatch.Application.Auditing.Services;
using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Application.Preferences.Commands;
using LMS.Dispatch.Application.Preferences.Ports;
using LMS.Dispatch.Domain.Preferences;

namespace LMS.Dispatch.Application.Preferences.Handlers;

// 司机本人保存偏好：读取、版本检查、写入与审计在同一本地事务内完成。
// 首次保存（没有记录）可不带版本；带了非零版本则与"不存在"不一致，按版本冲突处理。已有记录时带版本必须相等；
// 内容没有变化时不写入，直接返回当前记录（幂等）。并发的首次写入由主键唯一性裁决，输家按版本冲突处理。
internal sealed class UpdateDriverPreferencesHandler(
    ITransactionRunner transactionRunner,
    IDriverRepository drivers,
    IDriverPreferencesRepository preferences,
    IAuditRecordRepository auditRecords,
    TimeProvider timeProvider) : IUpdateDriverPreferencesHandler
{
    private const string Operation = "driver-preferences.update";
    private const string ResourceType = "driver-preferences";

    public async Task<OperationResult<DriverPreferences>> HandleAsync(UpdateDriverPreferencesCommand command, CancellationToken cancellationToken)
    {
        var driver = await drivers.FindByIdAsync(command.DriverId, cancellationToken);
        if (driver is null)
        {
            return OperationResult<DriverPreferences>.Failed(OperationOutcome.NotFound);
        }

        try
        {
            return await transactionRunner.ExecuteAsync(
                (context, token) => SaveAsync(command, driver.PropertyId, context, token),
                cancellationToken);
        }
        catch (DuplicateRecordException)
        {
            return OperationResult<DriverPreferences>.Failed(OperationOutcome.VersionConflict);
        }
    }

    private async Task<OperationResult<DriverPreferences>> SaveAsync(
        UpdateDriverPreferencesCommand command,
        string propertyId,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var current = await preferences.FindAsync(command.DriverId, context, cancellationToken);
        if (command.ExpectedVersion > 0 && command.ExpectedVersion != (current?.Version ?? 0))
        {
            return OperationResult<DriverPreferences>.Failed(OperationOutcome.VersionConflict);
        }

        if (current is not null && current.Language == command.Language)
        {
            return OperationResult<DriverPreferences>.Succeeded(current);
        }

        var now = timeProvider.GetUtcNow();
        var saved = current is null
            ? await InsertAsync(command, now, context, cancellationToken)
            : await ReplaceAsync(current, command, now, context, cancellationToken);
        if (saved is null)
        {
            return OperationResult<DriverPreferences>.Failed(OperationOutcome.VersionConflict);
        }

        await auditRecords.InsertAsync(
            AuditRecordFactory.Updated(command.Context, Operation, ResourceType, command.DriverId, now, ["language"], propertyId),
            context,
            cancellationToken);

        return OperationResult<DriverPreferences>.Succeeded(saved);
    }

    private async Task<DriverPreferences> InsertAsync(
        UpdateDriverPreferencesCommand command,
        DateTimeOffset now,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var created = DriverPreferences.Create(command.DriverId, command.Language, now);
        await preferences.InsertAsync(created, context, cancellationToken);

        return created;
    }

    private async Task<DriverPreferences?> ReplaceAsync(
        DriverPreferences current,
        UpdateDriverPreferencesCommand command,
        DateTimeOffset now,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var updated = current.WithLanguage(command.Language, now);

        return await preferences.TryReplaceAsync(updated, current.Version, context, cancellationToken)
            ? updated with { Version = current.Version + 1 }
            : null;
    }
}
