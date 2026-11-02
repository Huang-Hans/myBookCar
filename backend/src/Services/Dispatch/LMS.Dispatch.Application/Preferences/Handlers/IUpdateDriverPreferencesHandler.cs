using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Preferences.Commands;
using LMS.Dispatch.Domain.Preferences;

namespace LMS.Dispatch.Application.Preferences.Handlers;

public interface IUpdateDriverPreferencesHandler
{
    Task<OperationResult<DriverPreferences>> HandleAsync(UpdateDriverPreferencesCommand command, CancellationToken cancellationToken);
}
