using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace LMS.BuildingBlocks.Http.Tracing;

public static class ApiHttpContextExtensions
{
    private const string TraceIdItemKey = "LMS.Api.TraceId";
    private const string ApiCodeItemKey = "LMS.Api.Code";

    // traceId 取自当前请求活动的 W3C trace id，因此能贯穿整条调用链。
    public static string GetTraceId(this HttpContext context)
    {
        if (context.Items.TryGetValue(TraceIdItemKey, out var existing) && existing is string traceId)
        {
            return traceId;
        }

        var created = CreateTraceId();
        context.Items[TraceIdItemKey] = created;
        return created;
    }

    // 本进程写出的响应体中的应用码；没有写出响应体时为 null。
    public static int? GetApiCode(this HttpContext context) =>
        context.Items.TryGetValue(ApiCodeItemKey, out var code) && code is int value ? value : null;

    internal static void SetApiCode(this HttpContext context, int code) => context.Items[ApiCodeItemKey] = code;

    private static string CreateTraceId() =>
        Activity.Current is { IdFormat: ActivityIdFormat.W3C } activity
            ? activity.TraceId.ToHexString()
            : ActivityTraceId.CreateRandom().ToHexString();
}
