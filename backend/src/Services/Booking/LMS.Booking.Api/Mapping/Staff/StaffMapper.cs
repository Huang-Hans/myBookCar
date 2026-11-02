using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Requests;
using LMS.Booking.Api.Contracts.ConsoleNotifications;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Staff.Handlers;
using LMS.Booking.Domain.Staff;

namespace LMS.Booking.Api.Mapping.Staff;

internal static class StaffMapper
{
    public static StaffPreferencesView ToView(StaffPreferences preferences) => new(
        preferences.Station,
        WireEnum.ToWire(preferences.DefaultNotificationChannel),
        preferences.ConfirmationLanguage,
        preferences.EnabledNotificationTypes.ToList(),
        preferences.FavoriteLocationIds.ToList(),
        preferences.RecentLocationIds.ToList());

    public static RequestParseResult<(string Station, StaffNotificationChannel Channel, string Language)> ToSettings(
        ConsoleSettingsRequest request)
    {
        var errors = new FieldErrorCollector();
        var settings = (
            errors.RequiredText("station", request.Station),
            errors.RequiredEnum<StaffNotificationChannel>("defaultNotificationChannel", request.DefaultNotificationChannel),
            errors.RequiredText("confirmationLanguage", request.ConfirmationLanguage is null ? "EN" : request.ConfirmationLanguage));

        return errors.HasErrors
            ? RequestParseResult<(string, StaffNotificationChannel, string)>.Fail(errors.ToFailure())
            : RequestParseResult<(string, StaffNotificationChannel, string)>.Success(settings);
    }

    public static RequestParseResult<IReadOnlyList<string>> ToNotificationTypes(NotificationPreferencesRequest request)
    {
        var errors = new FieldErrorCollector();
        var types = RequiredList(errors, "enabledNotificationTypes", request.EnabledNotificationTypes);

        return errors.HasErrors
            ? RequestParseResult<IReadOnlyList<string>>.Fail(errors.ToFailure())
            : RequestParseResult<IReadOnlyList<string>>.Success(types);
    }

    public static RequestParseResult<IReadOnlyList<string>> ToLocationIds(LocationFavoritesRequest request)
    {
        var errors = new FieldErrorCollector();
        var ids = RequiredList(errors, "favoriteLocationIds", request.FavoriteLocationIds);

        return errors.HasErrors
            ? RequestParseResult<IReadOnlyList<string>>.Fail(errors.ToFailure())
            : RequestParseResult<IReadOnlyList<string>>.Success(ids);
    }

    private static IReadOnlyList<string> RequiredList(FieldErrorCollector errors, string field, IReadOnlyList<string>? values)
    {
        if (values is null)
        {
            errors.Add(field, "Required");
            return [];
        }

        if (values.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add(field, "Must contain non-blank values only");
        }

        return values.ToList();
    }
}
