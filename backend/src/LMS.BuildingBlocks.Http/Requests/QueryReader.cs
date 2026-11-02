using System.Globalization;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Errors;
using LMS.BuildingBlocks.Http.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace LMS.BuildingBlocks.Http.Requests;

// 严格读取查询串：只接受已声明并被读取的参数，未知参数、重复参数和非法值都汇总为 40001。
// 用法：依次读取所需参数，最后调用 Failure()，非 null 即应直接返回。
public sealed class QueryReader
{
    private readonly HttpRequest request;
    private readonly IQueryCollection query;
    private readonly HashSet<string> consumed = new(StringComparer.OrdinalIgnoreCase);
    private readonly FieldErrorCollector errors = new();

    private QueryReader(HttpRequest request)
    {
        this.request = request;
        query = request.Query;
    }

    public static QueryReader For(HttpContext context) => new(context.Request);

    internal string? OptionalUnrestrictedText(string name) => ReadText(name)?.Trim();

    internal int? OptionalInteger(string name, string errorMessage)
    {
        var text = ReadText(name);
        if (text is null)
        {
            return null;
        }

        return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            ? value
            : AddError<int?>(name, errorMessage);
    }

    internal long? OptionalLong(string name, string errorMessage)
    {
        var text = ReadText(name);
        if (text is null)
        {
            return null;
        }

        return long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            ? value
            : AddError<long?>(name, errorMessage);
    }

    internal IReadOnlyList<SortTerm> ParseSort(string? text, ListQuerySpec spec) => SortParser.Parse(text, spec, errors);

    public ListQuery ReadList(ListQuerySpec spec) =>
        new(ReadPage(), ReadPageSize(), (ReadText("search") ?? string.Empty).Trim(), SortParser.Parse(ReadText("sort"), spec, errors));

    // 不带分页和排序的查询（例如汇总）只读取 search，空白表示不筛选。
    public string ReadSearch() => (ReadText("search") ?? string.Empty).Trim();

    // 供接入层登记跨参数的校验问题（例如 from 必须早于 to）。
    public void Reject(string field, string message) => errors.Add(field, message);

    // 存在但为空白的参数视为非法，而不是“不筛选”。
    public string? OptionalText(string name)
    {
        var text = ReadText(name);
        if (text is not null && string.IsNullOrWhiteSpace(text))
        {
            errors.Add(name, "Must not be blank");
            return null;
        }

        return text?.Trim();
    }

    public bool? OptionalBool(string name)
    {
        var text = ReadText(name);
        return text switch
        {
            null => null,
            "true" => true,
            "false" => false,
            _ => AddError<bool?>(name, "Must be true or false")
        };
    }

    public T? OptionalEnum<T>(string name) where T : struct, Enum
    {
        var text = ReadText(name);
        if (text is null)
        {
            return null;
        }

        return WireEnum.TryParse<T>(text, out var value)
            ? value
            : AddError<T?>(name, $"Must be one of: {string.Join(", ", WireEnum.WireNames<T>())}");
    }

    public DateTimeOffset? OptionalUtcInstant(string name)
    {
        var text = ReadText(name);
        if (text is null)
        {
            return null;
        }

        const DateTimeStyles utcStyles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;
        if (text.EndsWith('Z') && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, utcStyles, out var instant))
        {
            return instant;
        }

        return AddError<DateTimeOffset?>(name, "Must be an ISO 8601 UTC date-time ending with Z");
    }

    // 校验完成后调用。GET 不接收正文，携带正文是请求结构错误（40000）；其后登记未知参数，
    // 再返回汇总的字段校验失败（40001）。无错误时为 null。
    // allowBody 用于带正文的动作（例如上传内容的 PUT），其查询串仍按同样的规则校验。
    public IResult? Failure(bool allowBody = false)
    {
        if (!allowBody && HasBody())
        {
            return ApiResults.Fail(ApiErrors.MalformedRequest, [new ApiFieldError(string.Empty, "A request body is not allowed")]);
        }

        RegisterUnknownParameters();

        return errors.HasErrors ? errors.ToFailure() : null;
    }

    private bool HasBody() => request.ContentLength > 0 || request.Headers.ContainsKey(HeaderNames.TransferEncoding);

    private void RegisterUnknownParameters()
    {
        foreach (var key in query.Keys.Where(key => !consumed.Contains(key)))
        {
            errors.Add(key, "Unknown query parameter");
        }
    }

    private int ReadPage()
    {
        var text = ReadText("page");
        if (text is null)
        {
            return PageSizes.DefaultPage;
        }

        var isValid = int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var page) && page >= 1;
        return isValid ? page : AddError("page", "Must be a positive integer", PageSizes.DefaultPage);
    }

    private int ReadPageSize()
    {
        var text = ReadText("pageSize");
        if (text is null)
        {
            return PageSizes.DefaultPageSize;
        }

        var isValid = int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var size) && PageSizes.IsAllowed(size);
        return isValid ? size : AddError("pageSize", "Must be one of: 10, 20, 50", PageSizes.DefaultPageSize);
    }

    private string? ReadText(string name)
    {
        consumed.Add(name);
        if (!query.TryGetValue(name, out var values))
        {
            return null;
        }

        return values.Count == 1 ? values[0] : AddError<string?>(name, "Must be provided exactly once");
    }

    private T AddError<T>(string field, string message, T fallback)
    {
        errors.Add(field, message);
        return fallback;
    }

    private T? AddError<T>(string field, string message)
    {
        errors.Add(field, message);
        return default;
    }
}
