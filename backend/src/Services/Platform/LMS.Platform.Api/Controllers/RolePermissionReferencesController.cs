using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Errors;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Platform.Api.Contracts.Configuration;
using LMS.Platform.Api.Mapping.Configuration;
using LMS.Platform.Application.Configuration.Handlers;
using LMS.Platform.Application.Configuration.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Platform.Api.Controllers;

/// <summary>
/// 角色权限参考（权限矩阵）接口。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("权限参考")]
[Route("api/platform/staff/v1/role-permission-references")]
public sealed class RolePermissionReferencesController(IConfigurationListHandler<RolePermissionReference> handler) : ControllerBase
{
    /// <summary>
    /// CF18 查询权限矩阵。
    /// </summary>
    /// <remarks>
    /// 矩阵不分页。权限矩阵只读展示；propertyId 仅用于确认调用者有权按该物业范围查看，不改变矩阵内容。
    /// </remarks>
    /// <returns>权限矩阵；矩阵不分页，因此没有 meta.pagination。</returns>
    [HttpGet(Name = "listRolePermissionReferences")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<RolePermissionView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ConfigurationRead)]
    public async Task<IResult> ListAsync([FromQuery] RolePermissionReferenceListRequest request, CancellationToken cancellationToken)
    {
        if (request.PropertyId is not null && !HttpContext.GetCaller().Scope.Allows(request.PropertyId))
        {
            return ApiResults.Fail(ApiErrors.InsufficientPermission);
        }

        var result = await handler.ListAllAsync(null, cancellationToken);

        return result.IsSuccess
            ? ApiResults.Ok(result.Value!.Select(ConfigurationViewMapper.ToView).ToList())
            : OperationResultMapper.ToFailure(result);
    }
}
