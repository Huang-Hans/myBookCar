using LMS.Booking.Application.Console.Options;
using LMS.Booking.Domain.Trips;
using LMS.BuildingBlocks.Core.Wire;
using Microsoft.Extensions.Options;

namespace LMS.Booking.Application.Trips.Services;

// 车型容量与积分价（部署配置，实施基线 D4）。客服代订与客户自助预订共用，避免两处各读一份。
internal sealed class VehicleCapacityResolver(IOptions<ConsoleOptions> options)
{
    public VehicleCapacity CapacityOf(VehicleClass vehicleClass)
    {
        var code = WireEnum.ToWire(vehicleClass);
        var option = options.Value.EffectiveVehicleClasses.FirstOrDefault(item => item.Code == code)
            ?? throw new InvalidOperationException($"Vehicle class '{code}' is not configured.");

        return new VehicleCapacity(vehicleClass, option.MaxPassengers, option.MaxLuggage, option.PointsRequired);
    }
}
