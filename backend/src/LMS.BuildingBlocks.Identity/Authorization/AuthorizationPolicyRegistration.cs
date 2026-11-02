using System.Security.Claims;
using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Identity.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace LMS.BuildingBlocks.Identity.Authorization;

internal static class AuthorizationPolicyRegistration
{
    // 默认策略（普通 [Authorize]）一律拒绝：业务入口必须显式声明身份策略，不能无意接受任意认证方案。
    public static IServiceCollection AddLmsAuthorizationPolicies(this IServiceCollection services) =>
        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder().RequireAssertion(_ => false).Build();
            options.AddPolicy(AuthorizationPolicies.Staff, policy => policy
                .AddAuthenticationSchemes(AuthenticationSchemes.StaffBearer)
                .RequireAuthenticatedUser());
            options.AddPolicy(AuthorizationPolicies.Driver, policy => RequirePrincipalType(policy, PrincipalType.Driver));
            options.AddPolicy(AuthorizationPolicies.Customer, policy => RequirePrincipalType(policy, PrincipalType.Customer));
            options.AddPolicy(AuthorizationPolicies.PlatformService, policy => policy
                .AddAuthenticationSchemes(AuthenticationSchemes.LmsAppBearer)
                .RequireAuthenticatedUser()
                .RequireRole(AppServiceIdentity.PrincipalType)
                .RequireClaim(AppTokenClaims.PrincipalType, AppServiceIdentity.PrincipalType)
                .RequireClaim(AppTokenClaims.Subject, AppServiceIdentity.PlatformSubject));
            options.AddPolicy(AuthorizationPolicies.NotificationRecipientService, policy =>
                RequireBookingService(policy, ServiceScopes.NotificationRecipientsQuery));
            options.AddPolicy(AuthorizationPolicies.DriverReleaseService, policy =>
                RequireBookingService(policy, ServiceScopes.DriverAvailabilityRelease));
            options.AddPolicy(AuthorizationPolicies.DriverContactService, policy =>
                RequireBookingService(policy, ServiceScopes.DriverContactRead));
            options.AddPolicy(AuthorizationPolicies.CouponRedemptionService, policy =>
                RequireBookingService(policy, ServiceScopes.CouponRedemptionWrite));
            options.AddPolicy(AuthorizationPolicies.ReportingSourceService, policy =>
                RequireService(policy, AppServiceIdentity.ReportingSubject, ServiceScopes.ReportingSourceRead));
        });

    // Booking 签发的受限服务令牌（ServiceBearer），且 scope 含指定范围。
    private static void RequireBookingService(AuthorizationPolicyBuilder policy, string scope) =>
        RequireService(policy, AppServiceIdentity.BookingSubject, scope);

    // 指定服务签发的受限服务令牌（ServiceBearer），且 scope 含指定范围。
    private static void RequireService(AuthorizationPolicyBuilder policy, string subject, string scope) =>
        policy.AddAuthenticationSchemes(AuthenticationSchemes.ServiceBearer)
            .RequireAuthenticatedUser()
            .RequireRole(AppServiceIdentity.PrincipalType)
            .RequireClaim(AppTokenClaims.PrincipalType, AppServiceIdentity.PrincipalType)
            .RequireClaim(AppTokenClaims.Subject, subject)
            .RequireAssertion(context => HasScope(context.User, scope));

    // scope 声明是空格分隔的范围列表。
    private static bool HasScope(ClaimsPrincipal user, string scope) =>
        user.FindFirstValue(AppTokenClaims.Scope)?.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(scope) == true;

    private static void RequirePrincipalType(AuthorizationPolicyBuilder policy, PrincipalType type)
    {
        var wire = WireEnum.ToWire(type);
        policy.AddAuthenticationSchemes(AuthenticationSchemes.LmsAppBearer)
            .RequireAuthenticatedUser()
            .RequireRole(wire)
            .RequireClaim(AppTokenClaims.PrincipalType, wire);
    }
}
