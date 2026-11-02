using LMS.BuildingBlocks.Core.Preferences;
using LMS.Dispatch.Application.Abstractions;

namespace LMS.Dispatch.Application.Preferences.Commands;

// 司机本人修改偏好。DriverId 来自令牌；ExpectedVersion 为 0 表示不比较版本（首次保存可不带版本）。
public sealed record UpdateDriverPreferencesCommand(string DriverId, AppLanguage Language, long ExpectedVersion, CommandContext Context);
