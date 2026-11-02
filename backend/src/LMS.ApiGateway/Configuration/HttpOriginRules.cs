namespace LMS.ApiGateway.Configuration;

internal static class HttpOriginRules
{
    // Origin 仅由协议、主机和可选端口组成：不含路径、查询和片段。
    public static bool IsHttpOrigin(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && IsHttpScheme(uri)
        && uri.AbsolutePath == "/"
        && uri.Query.Length == 0
        && uri.Fragment.Length == 0
        && !value.EndsWith('/');

    // 子域通配符来源（如 https://*.weprogram.site）。Uri 不接受 * 作为主机标签，
    // 因此替换成固定标签后复用同一套 Origin 校验，并要求通配符后面至少是一个带点的域名。
    public static bool IsWildcardSubdomainOrigin(string value)
    {
        const string marker = "://*.";
        var index = value.IndexOf(marker, StringComparison.Ordinal);
        if (index < 0)
        {
            return false;
        }

        var suffix = value[(index + marker.Length)..];

        return suffix.Contains('.', StringComparison.Ordinal)
            && IsHttpOrigin(string.Concat(value[..index], "://wildcard.", suffix));
    }

    public static bool IsAllowedOrigin(string value) =>
        IsHttpOrigin(value) || IsWildcardSubdomainOrigin(value);

    private static bool IsHttpScheme(Uri uri) =>
        uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
}
