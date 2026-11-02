using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Domain.Preferences;

namespace LMS.Dispatch.Application.Preferences.Handlers;

public interface IDriverPreferencesQueryHandler
{
    // 返回真实记录；从未保存过时返回契约默认（版本 0），不写库。
    Task<OperationResult<DriverPreferences>> GetAsync(string driverId, CancellationToken cancellationToken);
}
