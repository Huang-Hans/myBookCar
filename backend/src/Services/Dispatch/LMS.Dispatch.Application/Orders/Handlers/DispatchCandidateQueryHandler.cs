using LMS.Dispatch.Domain.Vehicles;
using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Dispatch.Application.Directory;
using LMS.Dispatch.Application.Drivers.Models;
using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Application.Offers.Ports;
using LMS.Dispatch.Application.Orders.Services;
using LMS.Dispatch.Domain.Drivers;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Shifts.Services;

namespace LMS.Dispatch.Application.Orders.Handlers;

// 司机下拉的候选：物业、状态、可用性由服务端筛选（规则与创建邀请共用）。
// Vehicle eligibility is applied before pagination using the current binding.
internal sealed class DispatchCandidateQueryHandler(
    IBookingDispatchOrders bookings,
    IDriverOfferRepository offers,
    IDriverRepository drivers,
    DispatchOrderComposer composer,
    PickupShiftQuery pickupShifts,
    TimeProvider timeProvider) : IDispatchCandidateQueryHandler
{
    public async Task<OperationResult<PageResult<Driver>>> ListAsync(string tripId, ListQuery list, CancellationToken cancellationToken)
    {
        var order = await bookings.GetAsync(tripId, cancellationToken);
        if (!order.IsSuccess)
        {
            return OperationResult<PageResult<Driver>>.Failed(OrderOutcomes.Of(order.Outcome));
        }

        var latest = await offers.FindLatestForTripAsync(tripId, cancellationToken);
        if (!composer.Compose(order.Value!, latest).CanDispatch)
        {
            return OperationResult<PageResult<Driver>>.Failed(OperationOutcome.StateConflict);
        }

        return OperationResult<PageResult<Driver>>.Succeeded(await ListForPickupAsync(order.Value!, list, cancellationToken));
    }

    private async Task<PageResult<Driver>> ListForPickupAsync(BookingOrder order, ListQuery list, CancellationToken cancellationToken)
    {
        var propertyId = order.PropertyId;
        var slot = pickupShifts.Resolve(order.PickupAt);
        var scheduled = await pickupShifts.ListAsync(slot, PropertyScope.ForProperty(propertyId), propertyId, cancellationToken);
        var held = await offers.ListHeldDriverIdsAsync(propertyId, timeProvider.GetUtcNow(), cancellationToken);
        var query = new DispatchCandidateQuery(list, propertyId, held, BookingVehicleGrades.FromClass(order.VehicleClass))
        {
            ScheduledDriverIds = scheduled.Select(shift => shift.DriverId).Distinct(StringComparer.Ordinal).ToList()
        };
        return await drivers.ListDispatchCandidatesAsync(query, cancellationToken);
    }
}
