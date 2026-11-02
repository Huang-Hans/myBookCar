using LMS.Booking.Application.Abstractions;
using LMS.Booking.Domain.Staff;
using LMS.BuildingBlocks.Core.Operations;

namespace LMS.Booking.Application.Staff.Handlers;

public interface IStaffPreferencesQueryHandler
{
    Task<StaffPreferences> GetAsync(string userId, CancellationToken cancellationToken);
}

public interface IStaffConsoleSettingsUpdateHandler
{
    Task<OperationResult<StaffPreferences>> UpdateSettingsAsync(
        string userId, string station, StaffNotificationChannel channel, string confirmationLanguage, CommandContext context, CancellationToken cancellationToken);
}

public interface IStaffNotificationPreferencesUpdateHandler
{
    Task<OperationResult<StaffPreferences>> UpdateNotificationPreferencesAsync(
        string userId, IReadOnlyList<string> enabledNotificationTypes, CommandContext context, CancellationToken cancellationToken);
}

public interface IStaffLocationFavoritesUpdateHandler
{
    Task<OperationResult<StaffPreferences>> UpdateFavoritesAsync(
        string userId, IReadOnlyList<string> locationIds, CommandContext context, CancellationToken cancellationToken);
}
