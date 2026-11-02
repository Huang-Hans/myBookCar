namespace LMS.Platform.Api.Contracts.Locations;

/// <summary>
/// 地点排序结果项，返回提交后的排序位置与新版本。
/// </summary>
/// <param name="LocationId">地点标识。</param>
/// <param name="SortOrder">该地点在区域内的新排序位置。</param>
/// <param name="Version">提交后的乐观锁版本号。</param>
public sealed record LocationOrderResultView(string LocationId, int SortOrder, long Version);
