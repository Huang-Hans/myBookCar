using System.Text.Json;
using System.Text.RegularExpressions;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Errors;
using LMS.BuildingBlocks.Http.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace LMS.BuildingBlocks.Http.Requests;

// 读取并严格反序列化 JSON 请求正文。语法错误或空正文为 40000，字段级问题（未知字段、类型不符）为 40001。
public static partial class JsonRequestBody
{
    private const int MaxBodyBytes = 256 * 1024;
    private const string JsonMediaType = "application/json";

    public static async Task<RequestParseResult<T>> ReadAsync<T>(HttpContext context, CancellationToken cancellationToken)
        where T : class
    {
        EnsureJsonContentType(context.Request);
        LimitBodySize(context);

        var document = await TryParseAsync(context.Request.Body, cancellationToken);
        if (document is null)
        {
            return RequestParseResult<T>.Fail(ApiResults.Fail(ApiErrors.MalformedRequest));
        }

        using (document)
        {
            return Deserialize<T>(document.RootElement);
        }
    }

    private static void EnsureJsonContentType(HttpRequest request)
    {
        if (!MediaTypeIsJson(request.ContentType))
        {
            throw new BadHttpRequestException("Unsupported media type", StatusCodes.Status415UnsupportedMediaType);
        }
    }

    private static bool MediaTypeIsJson(string? contentType) =>
        contentType is not null && contentType.AsSpan().TrimStart().StartsWith(JsonMediaType, StringComparison.OrdinalIgnoreCase);

    private static void LimitBodySize(HttpContext context)
    {
        var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (feature is { IsReadOnly: false })
        {
            feature.MaxRequestBodySize = MaxBodyBytes;
        }
    }

    private static async Task<JsonDocument?> TryParseAsync(Stream body, CancellationToken cancellationToken)
    {
        try
        {
            return await JsonDocument.ParseAsync(body, RequestJson.DocumentOptions, cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static RequestParseResult<T> Deserialize<T>(JsonElement root) where T : class
    {
        try
        {
            var value = root.Deserialize<T>(RequestJson.Options);
            return value is null
                ? RequestParseResult<T>.Fail(ApiResults.Fail(ApiErrors.MalformedRequest))
                : RequestParseResult<T>.Success(value);
        }
        catch (JsonException exception)
        {
            return RequestParseResult<T>.Fail(ApiResults.ValidationFailed([ToFieldError(exception)]));
        }
    }

    private static ApiFieldError ToFieldError(JsonException exception)
    {
        var path = NormalizePath(exception.Path);
        var unknown = UnknownMemberPattern().Match(exception.Message);

        return unknown.Success
            ? new ApiFieldError(Combine(path, unknown.Groups[1].Value), "Unknown field")
            : new ApiFieldError(path, "Invalid value");
    }

    private static string NormalizePath(string? path) =>
        string.IsNullOrEmpty(path) || path == "$" ? string.Empty : path.TrimStart('$').TrimStart('.');

    private static string Combine(string path, string member) => path.Length == 0 ? member : $"{path}.{member}";

    [GeneratedRegex("The JSON property '([^']+)' could not be mapped", RegexOptions.CultureInvariant)]
    private static partial Regex UnknownMemberPattern();
}
