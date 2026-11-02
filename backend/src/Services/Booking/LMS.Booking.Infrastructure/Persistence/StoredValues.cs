using System.Globalization;
using LMS.BuildingBlocks.Core.Wire;

namespace LMS.Booking.Infrastructure.Persistence;

// 枚举字面值与业务日期（YYYY-MM-DD 字符串）的存储转换。
internal static class StoredValues
{
    private const string DateFormat = "yyyy-MM-dd";

    public static T ParseEnum<T>(string text, string field) where T : struct, Enum =>
        WireEnum.TryParse<T>(text, out var value) ? value : throw new InvalidStoredDataException($"Unknown {field} value '{text}'.");

    public static string FromDate(DateOnly date) => date.ToString(DateFormat, CultureInfo.InvariantCulture);

    public static DateOnly ToDate(string text, string field) =>
        DateOnly.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : throw new InvalidStoredDataException($"Invalid {field} date '{text}'.");
}
