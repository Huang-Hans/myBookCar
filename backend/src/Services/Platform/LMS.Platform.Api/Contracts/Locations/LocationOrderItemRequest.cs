namespace LMS.Platform.Api.Contracts.Locations;

/// <summary>
/// 地点排序项：给出地点标识与其读到的版本，用于整体排序时的逐条并发校验。
/// </summary>
/// <param name="LocationId">本区域内的地点标识，必填且非空白。</param>
/// <param name="ExpectedVersion">提交者读到的记录版本（>=1），必填。</param>
public sealed record LocationOrderItemRequest(string? LocationId, long? ExpectedVersion);
