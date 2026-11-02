namespace LMS.BuildingBlocks.Http.Contracts;

/// <summary>
/// 字段级错误。
/// </summary>
/// <param name="Field">字段路径：嵌套用点号（address.postalCode），数组用下标（items[0].sku）；非字段错误为空字符串。</param>
/// <param name="Message">可展示的英文校验信息。</param>
/// <param name="Code">可选的字段错误码；没有时省略该键。</param>
public sealed record ApiFieldError(string Field, string Message, string? Code = null);
