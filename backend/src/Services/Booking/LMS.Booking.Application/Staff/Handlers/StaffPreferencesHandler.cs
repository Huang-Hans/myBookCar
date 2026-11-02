using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Auditing.Ports;
using LMS.Booking.Application.Auditing.Services;
using LMS.Booking.Application.Catalog.Ports;
using LMS.Booking.Application.Directory;
using LMS.Booking.Application.Staff.Ports;
using LMS.Booking.Domain.Staff;
using LMS.BuildingBlocks.Core.Operations;

namespace LMS.Booking.Application.Staff.Handlers;

// 员工偏好（Console Settings / 通知偏好 / 地点收藏，均本人自助，按 userId 定位）。
// 读取缺失时返回默认值；写入整体替换，与审计记录同事务提交。
internal sealed class StaffPreferencesHandler(
    IStaffPreferencesRepository preferences,
    IPlatformCatalog catalog,
    IAuditRecordRepository auditRecords,
    ITransactionRunner transactionRunner,
    TimeProvider timeProvider) :
    IStaffPreferencesQueryHandler,
    IStaffConsoleSettingsUpdateHandler,
    IStaffNotificationPreferencesUpdateHandler,
    IStaffLocationFavoritesUpdateHandler
{
    private const string ResourceType = "staff-preferences";

    public async Task<StaffPreferences> GetAsync(string userId, CancellationToken cancellationToken) =>
        await preferences.FindAsync(userId, cancellationToken) ?? StaffPreferences.Defaults(userId);

    public Task<OperationResult<StaffPreferences>> UpdateSettingsAsync(
        string userId,
        string station,
        StaffNotificationChannel channel,
        string confirmationLanguage,
        CommandContext context,
        CancellationToken cancellationToken)
    {
        var issues = StaffPreferences.Validate(station, StaffPreferences.AllNotificationTypes, [], []);
        if (issues.Count > 0)
        {
            return Task.FromResult(OperationResult<StaffPreferences>.Invalid(issues));
        }

        return UpsertAsync(userId, current => current with
        {
            Station = station,
            DefaultNotificationChannel = channel,
            ConfirmationLanguage = confirmationLanguage
        }, "staff-preferences.update-settings", context, cancellationToken);
    }

    public Task<OperationResult<StaffPreferences>> UpdateNotificationPreferencesAsync(
        string userId,
        IReadOnlyList<string> enabledNotificationTypes,
        CommandContext context,
        CancellationToken cancellationToken)
    {
        var issues = StaffPreferences.Validate(string.Empty, enabledNotificationTypes, [], []);
        if (issues.Count > 0)
        {
            return Task.FromResult(OperationResult<StaffPreferences>.Invalid(issues));
        }

        return UpsertAsync(userId, current => current with { EnabledNotificationTypes = enabledNotificationTypes.ToList() },
            "staff-preferences.update-notification-preferences", context, cancellationToken);
    }

    public async Task<OperationResult<StaffPreferences>> UpdateFavoritesAsync(
        string userId,
        IReadOnlyList<string> locationIds,
        CommandContext context,
        CancellationToken cancellationToken)
    {
        var issues = StaffPreferences.Validate(string.Empty, StaffPreferences.AllNotificationTypes, locationIds, []);
        if (issues.Count > 0)
        {
            return OperationResult<StaffPreferences>.Invalid(issues);
        }

        // 每个收藏地点必须存在且启用（Platform 目录实时核对）；源不可用一律 503。
        foreach (var locationId in locationIds)
        {
            var location = await catalog.GetLocationAsync(locationId, cancellationToken);
            if (!location.IsSuccess)
            {
                return location.Outcome switch
                {
                    DirectoryOutcome.NotFound => OperationResult<StaffPreferences>.Rejected(
                        [new FieldIssue("favoriteLocationIds", $"Location '{locationId}' does not exist")]),
                    DirectoryOutcome.Forbidden => OperationResult<StaffPreferences>.Failed(OperationOutcome.Forbidden),
                    _ => OperationResult<StaffPreferences>.Failed(OperationOutcome.SourceUnavailable)
                };
            }

            if (location.Value is not { IsActive: true })
            {
                return OperationResult<StaffPreferences>.Rejected(
                    [new FieldIssue("favoriteLocationIds", $"Location '{locationId}' is not active")]);
            }
        }

        return await UpsertAsync(userId, current => current with { FavoriteLocationIds = locationIds.ToList() },
            "staff-preferences.update-favorites", context, cancellationToken);
    }

    private async Task<OperationResult<StaffPreferences>> UpsertAsync(
        string userId,
        Func<StaffPreferences, StaffPreferences> apply,
        string operation,
        CommandContext context,
        CancellationToken cancellationToken)
    {
        var current = await preferences.FindAsync(userId, cancellationToken) ?? StaffPreferences.Defaults(userId);
        var updated = apply(current);

        await transactionRunner.ExecuteAsync(
            async (transaction, token) =>
            {
                await preferences.UpsertAsync(updated, transaction, token);
                await auditRecords.InsertAsync(
                    AuditRecordFactory.Succeeded(context, operation, ResourceType, updated.UserId, timeProvider.GetUtcNow(), operation, null, null),
                    transaction,
                    token);
                return 0;
            },
            cancellationToken);

        return OperationResult<StaffPreferences>.Succeeded(updated);
    }
}
