using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Application.Console.Options;
using LMS.Booking.Application.SelfService.Models;
using LMS.Booking.Application.Trips.Services;
using LMS.Booking.Domain.Guests;
using LMS.Booking.Domain.Trips;
using Microsoft.Extensions.Options;

namespace LMS.Booking.Application.SelfService.Services;

// 自助预订的车型目录：容量与积分价来自部署配置，等级权益矩阵与客服共用，但是否允许自助预订由 Platform 自助资格规则另行决定。
// 同一份逻辑既给 ride-options 展示，也给建单校验，避免展示可选而建单被拒。
internal sealed class SelfServiceVehicleCatalog(VehicleCapacityResolver capacityResolver, IOptions<ConsoleOptions> consoleOptions)
{
    private const string TierNotEntitled = "TIER_NOT_ENTITLED";
    private const string PassengersExceedCapacity = "PASSENGERS_EXCEED_CAPACITY";

    public IReadOnlyList<RideOption> OptionsFor(GuestProfile guest, int passengers, string vehicleClass)
    {
        if (!WireEnum.TryParse<VehicleClass>(vehicleClass, out var selected))
            throw new InvalidOperationException("The service product must map to a known vehicle class.");
        return [OptionOf(guest, selected, passengers)];
    }

    public OperationResult<VehicleCapacity> CheckSelection(GuestProfile guest, VehicleClass vehicleClass, int passengers, int luggage)
    {
        var option = OptionOf(guest, vehicleClass, passengers);
        if (!option.IsEligible)
        {
            var field = option.IneligibleReason == TierNotEntitled ? "vehicleClass" : "passengers";
            return OperationResult<VehicleCapacity>.Rejected([new FieldIssue(field, MessageOf(option.IneligibleReason!))]);
        }

        var capacity = capacityResolver.CapacityOf(vehicleClass);
        var issues = TripRules.Validate(vehicleClass, passengers, luggage, DateTimeOffset.MaxValue, false, DateTimeOffset.MinValue, 0, capacity);

        return issues.Count > 0 ? OperationResult<VehicleCapacity>.Invalid(issues) : OperationResult<VehicleCapacity>.Succeeded(capacity);
    }

    private RideOption OptionOf(GuestProfile guest, VehicleClass vehicleClass, int passengers)
    {
        var capacity = capacityResolver.CapacityOf(vehicleClass);
        var reason = IneligibleReasonOf(guest, vehicleClass, passengers, capacity);

        return new RideOption(vehicleClass, capacity.MaxPassengers, capacity.MaxLuggage, capacity.PointsRequired, reason is null, reason);
    }

    private string? IneligibleReasonOf(GuestProfile guest, VehicleClass vehicleClass, int passengers, VehicleCapacity capacity)
    {
        var entitlement = consoleOptions.Value.EffectiveTierEntitlements
            .FirstOrDefault(tier => string.Equals(tier.TierCode, guest.TierCode, StringComparison.OrdinalIgnoreCase));
        if (entitlement is null || !entitlement.VehicleClasses.Contains(WireEnum.ToWire(vehicleClass), StringComparer.Ordinal))
        {
            return TierNotEntitled;
        }

        return passengers > capacity.MaxPassengers ? PassengersExceedCapacity : null;
    }

    private static string MessageOf(string reason) => reason == TierNotEntitled
        ? "Guest tier is not entitled to the selected vehicle class"
        : "Passengers exceed the capacity of the selected vehicle class";
}
