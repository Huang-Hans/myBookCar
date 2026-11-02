using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.SelfService.Models;
using LMS.Booking.Application.SelfService.Options;
using LMS.Booking.Application.SelfService.Services;
using Microsoft.Extensions.Options;

namespace LMS.Booking.Application.SelfService.Handlers;

// 车型与路线查询：只读，不写任何资格、审批或配额记录。路线与车型的检查与建单使用同一份逻辑，因此这里可选的组合建单时不会因同一规则被拒。
internal sealed class CustomerRideOptionsHandler(
    GuestBookingGate gate,
    SelfServiceRouteChecker routeChecker,
    SelfServiceVehicleCatalog vehicles,
    IOptions<SelfServiceOptions> options) : ICustomerRideOptionsHandler
{
    public async Task<OperationResult<RideOptionsResult>> GetAsync(
        string guestId,
        string productCode,
        string pickupLocationId,
        string dropoffLocationId,
        int passengers,
        CancellationToken cancellationToken)
    {
        var product = options.Value.EffectiveProducts.FirstOrDefault(item => item.Code == productCode);
        if (product is null || !product.IsEnabled)
        {
            return OperationResult<RideOptionsResult>.Invalid([new FieldIssue("productCode", product is null ? "Unknown service product" : "The service product is not available")]);
        }

        var context = await gate.LoadAsync(guestId, cancellationToken);
        if (!context.IsSuccess)
        {
            return OperationResult<RideOptionsResult>.FailureOf(context);
        }

        var route = await routeChecker.CheckAsync(product, pickupLocationId, dropoffLocationId, cancellationToken);

        return route.IsSuccess
            ? OperationResult<RideOptionsResult>.Succeeded(new RideOptionsResult(route.Value!, vehicles.OptionsFor(context.Value!.Guest, passengers, product.VehicleClass)))
            : OperationResult<RideOptionsResult>.FailureOf(route);
    }
}
