using LMS.BuildingBlocks.Core.Notifications.Delivery;
using LMS.BuildingBlocks.Http.Extensions;
using LMS.BuildingBlocks.Realtime.Hubs;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Dispatch.Api.Authorization;
using LMS.Dispatch.Api.Background;
using LMS.Dispatch.Api.Notifications;
using LMS.BuildingBlocks.Identity.Authentication;
using LMS.Dispatch.Application.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace LMS.Dispatch.Api.Extensions;

public static class DispatchApiExtensions
{
    public static IServiceCollection AddDispatchApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DevelopmentAccessOptions>().Bind(configuration.GetSection(DevelopmentAccessOptions.SectionName));
        SuppressStaffChallengeWhenAnonymous(services);
        services.AddApiControllers();
        services.AddHttpContextAccessor();
        services.AddScoped<ICallerToken, HttpContextCallerToken>();
        services.AddScoped<PlatformCallerAuthorizationResolver>();
        services.AddScoped<ICallerAuthorizationResolver, DevelopmentCallerAuthorizationResolver>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, DevelopmentShiftAuthorizationResultHandler>();
        services.AddScoped<IPrincipalRecordVerifier, DriverPrincipalRecordVerifier>();
        services.AddHostedService<OfferExpirationWorker>();
        services.AddScoped<INotificationDeliverer, DispatchNotificationDeliverer>();
        services.AddScoped<INotificationConnectionVerifier, DispatchNotificationConnectionVerifier>();

        return services;
    }

    // 员工令牌缺失时，认证事件会直接写出 40100。本机放行必须在那之前把挑战吞掉，授权过滤器才有机会继续。
    private static void SuppressStaffChallengeWhenAnonymous(IServiceCollection services) =>
        services.PostConfigure<JwtBearerOptions>(AuthenticationSchemes.StaffBearer, options =>
        {
            var challenge = options.Events.OnChallenge;
            options.Events.OnChallenge = context =>
            {
                if (!DevelopmentStaffAccess.Allows(context.HttpContext))
                {
                    return challenge(context);
                }

                context.HandleResponse();
                return Task.CompletedTask;
            };
        });
}
