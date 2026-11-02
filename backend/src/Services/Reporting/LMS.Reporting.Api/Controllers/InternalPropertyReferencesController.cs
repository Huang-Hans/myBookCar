using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Core.References;
using LMS.BuildingBlocks.Http.Errors;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Reporting.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/v1/properties/{propertyId}/references")]
public sealed class InternalPropertyReferencesController(IPropertyReferenceReader references) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCodes.PropertiesWrite)]
    public async Task<IResult> GetAsync(string propertyId, CancellationToken cancellationToken)
    {
        if (!HttpContext.GetCaller().Scope.Allows(propertyId)) return ApiResults.Fail(ApiErrors.ResourceNotFound);
        return ApiResults.Ok(await references.HasReferencesAsync(propertyId, cancellationToken));
    }
}
