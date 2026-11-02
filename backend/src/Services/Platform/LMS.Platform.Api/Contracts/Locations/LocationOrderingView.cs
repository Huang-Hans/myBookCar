namespace LMS.Platform.Api.Contracts.Locations;

/// <summary>
/// 地点排序结果视图，返回该区域替换后的展示顺序。
/// </summary>
/// <param name="Region">区域代码，取四个 Region 字面值之一。</param>
/// <param name="Items">该区域的新地点顺序与各自版本。</param>
public sealed record LocationOrderingView(string Region, IReadOnlyList<LocationOrderResultView> Items);
