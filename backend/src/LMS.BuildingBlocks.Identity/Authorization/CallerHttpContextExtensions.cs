using Microsoft.AspNetCore.Http;

namespace LMS.BuildingBlocks.Identity.Authorization;

public static class CallerHttpContextExtensions
{
    private const string CallerItemKey = "LMS.Identity.Caller";

    // 仅可在标注了 RequirePermission / RequireCaller 的控制器操作内调用。
    public static CallerAuthorization GetCaller(this HttpContext context) =>
        context.Items.TryGetValue(CallerItemKey, out var caller) && caller is CallerAuthorization authorization
            ? authorization
            : throw new InvalidOperationException("The action does not require a caller; apply RequirePermission or RequireCaller.");

    internal static void SetCaller(this HttpContext context, CallerAuthorization caller) =>
        context.Items[CallerItemKey] = caller;
}
