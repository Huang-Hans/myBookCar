using System.Text.RegularExpressions;

namespace LMS.BuildingBlocks.Core.Security;

// 手机号登录标识的统一规范：优先接受带国家/地区码的完整号码，形如 +85361234567；
// 不带国家码时按位数推断：11 位以 1 开头按中国手机号补 +86，8 位按澳门号码补 +853，已是 86/853 开头的完整号码只补 +。
// 公开登录校验、内部凭据查询以及后端保存手机号都使用同一个函数，保证“请求规范化结果”与“库内 phone”可以精确相等。
// 只清理约定的展示字符（空白、连字符、圆括号），不做模糊匹配。
public static partial class PhoneNumber
{
    // E.164：+、非 0 国家码首位，合计 8～15 位数字。
    private const string CanonicalPattern = "^\\+[1-9][0-9]{7,14}$";

    // 不带国家码时允许的纯数字位数。
    private const string DigitsPattern = "^[0-9]{8,15}$";

    // 规范化成功返回 true；含非数字字符或位数不合法返回 false，normalized 为空串。
    public static bool TryNormalize(string? raw, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var compact = DisplaySeparators().Replace(raw, string.Empty);
        if (CanonicalForm().IsMatch(compact))
        {
            normalized = compact;
            return true;
        }

        if (!DigitsOnly().IsMatch(compact))
        {
            return false;
        }

        normalized = InferCountryCode(compact);

        return true;
    }

    private static string InferCountryCode(string digits)
    {
        if (digits.Length == 11 && digits[0] == '1')
        {
            return "+86" + digits;
        }

        if (digits.Length == 8)
        {
            return "+853" + digits;
        }

        if (digits.Length == 13 && digits.StartsWith("86", StringComparison.Ordinal))
        {
            return "+" + digits;
        }

        if (digits.Length == 11 && digits.StartsWith("853", StringComparison.Ordinal))
        {
            return "+" + digits;
        }

        return "+" + digits;
    }

    [GeneratedRegex("[\\s\\-()]", RegexOptions.CultureInvariant)]
    private static partial Regex DisplaySeparators();

    [GeneratedRegex(CanonicalPattern, RegexOptions.CultureInvariant)]
    private static partial Regex CanonicalForm();

    [GeneratedRegex(DigitsPattern, RegexOptions.CultureInvariant)]
    private static partial Regex DigitsOnly();
}
