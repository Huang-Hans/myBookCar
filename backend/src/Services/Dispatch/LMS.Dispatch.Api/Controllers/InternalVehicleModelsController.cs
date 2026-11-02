using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Dispatch.Application.Vehicles.Ports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Dispatch.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.BookingVehicleCatalogService)]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class InternalVehicleModelsController(IBookingVehicleModelReader vehicles) : ControllerBase
{
    [HttpGet("internal/v1/booking-vehicle-models")]
    public async Task<IResult> GetAsync([FromQuery] string propertyId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(propertyId))
            return ApiResults.ValidationFailed([new ApiFieldError("propertyId", "Required")]);
        Response.Headers.CacheControl = "no-store";
        return ApiResults.Ok(await vehicles.ListBookingModelsAsync(propertyId.Trim(), cancellationToken));
    }
}
