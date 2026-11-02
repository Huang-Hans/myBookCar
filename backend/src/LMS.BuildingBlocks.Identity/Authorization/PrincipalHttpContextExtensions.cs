using Microsoft.AspNetCore.Http;

namespace LMS.BuildingBlocks.Identity.Authorization;

public static class PrincipalHttpContextExtensions
{
    private const string PrincipalItemKey = "LMS.Identity.Principal";

    // 仅可在标注了 RequirePrincipal 的控制器操作内调用；本人标识只能从这里取得，不得读取请求参数。
    public static PrincipalAuthorization GetPrincipal(this HttpContext context) =>
        context.Items.TryGetValue(PrincipalItemKey, out var principal) && principal is PrincipalAuthorization authorization
            ? authorization
            : throw new InvalidOperationException("The action does not require a principal; apply RequirePrincipal.");

    internal static void SetPrincipal(this HttpContext context, PrincipalAuthorization principal) =>
        context.Items[PrincipalItemKey] = principal;
}
