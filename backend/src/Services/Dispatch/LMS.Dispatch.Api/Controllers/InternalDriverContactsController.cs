using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Errors;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Dispatch.Api.Contracts.Internal;
using LMS.Dispatch.Application.Drivers.Ports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Dispatch.Api.Controllers;

// Booking verifies trip ownership before calling this service-only endpoint.
[ApiController]
[Authorize(Policy = AuthorizationPolicies.DriverContactService)]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/v1/drivers")]
public sealed class InternalDriverContactsController(IDriverRepository drivers) : ControllerBase
{
    [HttpGet("{driverId}/contacts")]
    [ProducesResponseType(typeof(ApiEnvelope<DriverContactView>), StatusCodes.Status200OK)]
    public async Task<IResult> GetAsync(string driverId, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var driver = await drivers.FindByIdAsync(driverId, cancellationToken);
        return driver is null || string.IsNullOrWhiteSpace(driver.Phone)
            ? ApiResults.Fail(ApiErrors.ResourceNotFound)
            : ApiResults.Ok(new DriverContactView(driver.DriverId, driver.Phone));
    }
}
