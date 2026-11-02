using LMS.Booking.Api.Contracts.Trips;
using LMS.Booking.Application.Trips.Handlers;
using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Booking.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("客服行程")]
public sealed class ConsoleBookingOptionsController(IConsoleBookingOptionsHandler handler) : ControllerBase
{
    [HttpGet("api/booking/staff/v1/booking-options", Name = "getConsoleBookingOptions")]
    [RequirePermission(PermissionCodes.BookingsWrite)]
    [ProducesResponseType(typeof(ApiEnvelope<ConsoleBookingOptionsView>), StatusCodes.Status200OK)]
    public IResult Get()
    {
        Response.Headers.CacheControl = "no-store";
        var options = handler.Get();
        return ApiResults.Ok(new ConsoleBookingOptionsView(
            options.Products.Select(item => new ConsoleBookingProductView(
                item.Code, item.Name, item.IsEnabled, item.ServiceType, item.RouteScope, item.UnavailableReason, item.VehicleClass)).ToList(),
            options.Vehicles.Select(item => new ConsoleBookingVehicleView(item.Code, item.MaxPassengers, item.MaxLuggage)).ToList(),
            options.IsAsapAvailable));
    }
}
