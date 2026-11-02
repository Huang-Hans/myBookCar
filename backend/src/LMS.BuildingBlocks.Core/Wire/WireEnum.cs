using System.Text.Json;

namespace LMS.BuildingBlocks.Core.Wire;

// 枚举与对外字面值（SCREAMING_SNAKE_CASE）之间的显式转换，大小写严格，不接受数字。
public static class WireEnum
{
    public static string ToWire<T>(T value) where T : struct, Enum => Names<T>.ToWire[value];

    public static bool TryParse<T>(string? text, out T value) where T : struct, Enum
    {
        value = default;
        return text is not null && Names<T>.FromWire.TryGetValue(text, out value);
    }

    public static IReadOnlyCollection<string> WireNames<T>() where T : struct, Enum => Names<T>.FromWire.Keys;

    private static class Names<T> where T : struct, Enum
    {
        public static readonly Dictionary<T, string> ToWire =
            Enum.GetValues<T>().ToDictionary(value => value, value => JsonNamingPolicy.SnakeCaseUpper.ConvertName(value.ToString()));

        public static readonly Dictionary<string, T> FromWire =
            ToWire.ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.Ordinal);
    }
}
