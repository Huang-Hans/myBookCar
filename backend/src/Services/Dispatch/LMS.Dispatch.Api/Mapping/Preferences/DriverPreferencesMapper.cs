using LMS.BuildingBlocks.Core.Preferences;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Requests;
using LMS.Dispatch.Api.Contracts.Preferences;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Preferences.Commands;
using LMS.Dispatch.Domain.Preferences;

namespace LMS.Dispatch.Api.Mapping.Preferences;

internal static class DriverPreferencesMapper
{
    public static DriverPreferencesView ToView(DriverPreferences preferences) =>
        new(WireEnum.ToWire(preferences.Language), preferences.UpdatedAt, preferences.Version);

    public static RequestParseResult<UpdateDriverPreferencesCommand> ToCommand(
        string driverId,
        long expectedVersion,
        UpdateDriverPreferencesRequest request,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var language = errors.RequiredEnum<AppLanguage>("language", request.Language);

        return errors.HasErrors
            ? RequestParseResult<UpdateDriverPreferencesCommand>.Fail(errors.ToFailure())
            : RequestParseResult<UpdateDriverPreferencesCommand>.Success(
                new UpdateDriverPreferencesCommand(driverId, language, expectedVersion, context));
    }
}
