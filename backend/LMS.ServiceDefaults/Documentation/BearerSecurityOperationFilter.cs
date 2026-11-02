using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LMS.ServiceDefaults.Documentation;

// Document endpoint authorization without changing runtime access policies.
// 员工接口（Staff 策略）使用 bearerAuth（Keycloak 员工令牌）；司机/客户/服务接口使用 lmsAppBearer（LMS 本地令牌）。
internal sealed class BearerSecurityOperationFilter : IOperationFilter
{
    public const string StaffSchemeId = "bearerAuth";
    public const string AppSchemeId = "lmsAppBearer";

    // 与 LMS.BuildingBlocks.Identity 的 AuthorizationPolicies 保持一致：使用本地令牌的策略名。
    private static readonly string[] AppPolicies = ["Driver", "Customer", "PlatformService"];

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IAllowAnonymous>().Any() || !metadata.OfType<IAuthorizeData>().Any())
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = SchemeIdFor(metadata) }
            }] = []
        });
    }

    private static string SchemeIdFor(IList<object> metadata) =>
        metadata.OfType<IAuthorizeData>().Any(data => data.Policy is not null && AppPolicies.Contains(data.Policy))
            ? AppSchemeId
            : StaffSchemeId;
}
