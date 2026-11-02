using System.Globalization;
using System.Text.RegularExpressions;
using LMS.BuildingBlocks.Core.Patching;
using LMS.BuildingBlocks.Core.Wire;

namespace LMS.BuildingBlocks.Http.Requests;

// 请求字段的严格读取：成功时返回规范化后的值，失败时登记字段错误并返回占位值（调用方在汇总后统一返回 40001）。
// Required* 用于创建请求的必填字段；Patch* 用于局部更新，未传保持 IsSet=false，显式 null 只对可清空字段合法。
public static partial class FieldReaders
{
    private const string DateFormat = "yyyy-MM-dd";

    public static string RequiredText(this FieldErrorCollector errors, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(field, "Is required and must not be blank");
            return string.Empty;
        }

        return value.Trim();
    }

    public static T RequiredEnum<T>(this FieldErrorCollector errors, string field, string? value) where T : struct, Enum
    {
        if (WireEnum.TryParse<T>(value, out var parsed))
        {
            return parsed;
        }

        errors.Add(field, $"Must be one of: {string.Join(", ", WireEnum.WireNames<T>())}");
        return default;
    }

    public static bool RequiredBool(this FieldErrorCollector errors, string field, bool? value)
    {
        if (value is null)
        {
            errors.Add(field, "Is required");
            return false;
        }

        return value.Value;
    }

    public static double RequiredNumber(this FieldErrorCollector errors, string field, double? value)
    {
        if (value is null || !double.IsFinite(value.Value))
        {
            errors.Add(field, "Is required and must be a finite number");
            return 0;
        }

        return value.Value;
    }

    public static DateOnly RequiredDate(this FieldErrorCollector errors, string field, string? value)
    {
        if (TryParseDate(value, out var date))
        {
            return date;
        }

        errors.Add(field, "Must be a calendar date in YYYY-MM-DD format");
        return default;
    }

    public static Optional<string> PatchText(this FieldErrorCollector errors, string field, Optional<string?> value)
    {
        if (!value.IsSet)
        {
            return default;
        }

        return string.IsNullOrWhiteSpace(value.Value)
            ? AddError<string>(errors, field, "Must not be null or blank")
            : Optional<string>.Of(value.Value.Trim());
    }

    // 可清空的文本：显式 null 或空白表示清空。
    public static Optional<string> PatchNullableText(this Optional<string?> value) =>
        value.IsSet ? Optional<string>.Of(string.IsNullOrWhiteSpace(value.Value) ? null : value.Value.Trim()) : default;

    public static Optional<T> PatchEnum<T>(this FieldErrorCollector errors, string field, Optional<string?> value) where T : struct, Enum
    {
        if (!value.IsSet)
        {
            return default;
        }

        return WireEnum.TryParse<T>(value.Value, out var parsed)
            ? Optional<T>.Of(parsed)
            : AddError<T>(errors, field, $"Must be one of: {string.Join(", ", WireEnum.WireNames<T>())}");
    }

    public static Optional<bool> PatchBool(this FieldErrorCollector errors, string field, Optional<bool?> value)
    {
        if (!value.IsSet)
        {
            return default;
        }

        return value.Value is null ? AddError<bool>(errors, field, "Must be true or false") : Optional<bool>.Of(value.Value.Value);
    }

    public static Optional<double> PatchNumber(this FieldErrorCollector errors, string field, Optional<double?> value)
    {
        if (!value.IsSet)
        {
            return default;
        }

        return value.Value is null || !double.IsFinite(value.Value.Value)
            ? AddError<double>(errors, field, "Must be a finite number")
            : Optional<double>.Of(value.Value.Value);
    }

    public static Optional<DateOnly> PatchDate(this FieldErrorCollector errors, string field, Optional<string?> value)
    {
        if (!value.IsSet)
        {
            return default;
        }

        return TryParseDate(value.Value, out var date)
            ? Optional<DateOnly>.Of(date)
            : AddError<DateOnly>(errors, field, "Must be a calendar date in YYYY-MM-DD format");
    }

    public static int RequiredInt(this FieldErrorCollector errors, string field, int? value)
    {
        if (value is null)
        {
            errors.Add(field, "Is required");
            return 0;
        }

        return value.Value;
    }

    public static Optional<int> PatchInt(this FieldErrorCollector errors, string field, Optional<int?> value)
    {
        if (!value.IsSet)
        {
            return default;
        }

        return value.Value is null ? AddError<int>(errors, field, "Must be an integer") : Optional<int>.Of(value.Value.Value);
    }

    public static long RequiredPositiveLong(this FieldErrorCollector errors, string field, long? value)
    {
        if (value is null or <= 0)
        {
            errors.Add(field, "Must be a positive integer");
            return 0;
        }

        return value.Value;
    }

    public static string RequiredSha256(this FieldErrorCollector errors, string field, string? value)
    {
        if (value is null || !Sha256Pattern().IsMatch(value))
        {
            errors.Add(field, "Must be 64 lowercase hexadecimal characters");
            return string.Empty;
        }

        return value;
    }

    // 文件名只是展示用的原名，不是服务器路径：拒绝路径分隔符和控制字符。
    public static string RequiredFileName(this FieldErrorCollector errors, string field, string? value)
    {
        var name = errors.RequiredText(field, value);
        if (name.Any(character => character is '/' or '\\' || char.IsControl(character)))
        {
            errors.Add(field, "Must be a file name without path separators");
        }

        return name;
    }

    // 请求正文里的时刻与查询串一致：ISO 8601，必须带 Z（UTC）。
    public static DateTimeOffset RequiredUtcInstant(this FieldErrorCollector errors, string field, string? value)
    {
        const DateTimeStyles utcStyles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;
        if (value is not null && value.EndsWith('Z') && DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, utcStyles, out var instant))
        {
            return instant;
        }

        errors.Add(field, "Must be an ISO 8601 UTC date-time ending with Z");
        return default;
    }

    // 可选时刻：没传（null）表示不限，传了就必须是带 Z 的 ISO 8601 UTC 时刻。
    public static DateTimeOffset? OptionalUtcInstant(this FieldErrorCollector errors, string field, string? value) =>
        value is null ? null : errors.RequiredUtcInstant(field, value);

    public static string RequiredEmail(this FieldErrorCollector errors, string field, string? value)
    {
        var email = errors.RequiredText(field, value);
        if (email.Length > 0 && !EmailPattern().IsMatch(email))
        {
            errors.Add(field, "Must be a valid email address");
        }

        return email;
    }

    [GeneratedRegex("^[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Pattern();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();

    private static bool TryParseDate(string? value, out DateOnly date)
    {
        date = default;
        return value is not null && DateOnly.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private static Optional<T> AddError<T>(FieldErrorCollector errors, string field, string message)
    {
        errors.Add(field, message);
        return default;
    }
}
