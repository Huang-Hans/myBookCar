using Microsoft.Extensions.Options;

namespace LMS.Dispatch.Api.Authorization;

// Development 下，员工接口可以不带令牌。空白的 Authorization 或只有 "Bearer" 前缀，都视为没有令牌。
internal static class DevelopmentStaffAccess
{
    private const string StaffPath = "/api/dispatch/staff";

    public static bool Allows(HttpContext context, IHostEnvironment environment, DevelopmentAccessOptions options) =>
        environment.IsDevelopment()
        && options.AllowAnonymousStaff
        && context.Request.Path.StartsWithSegments(StaffPath)
        && !HasBearerToken(context);

    public static bool Allows(HttpContext context)
    {
        var environment = context.RequestServices.GetRequiredService<IHostEnvironment>();
        var options = context.RequestServices.GetRequiredService<IOptions<DevelopmentAccessOptions>>().Value;

        return Allows(context, environment, options);
    }

    private static bool HasBearerToken(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("Authorization", out var value))
        {
            return false;
        }

        var text = value.ToString().Trim();
        if (text.Length == 0)
        {
            return false;
        }

        const string prefix = "Bearer ";
        if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return text[prefix.Length..].Trim().Length > 0;
        }

        return true;
    }
}
