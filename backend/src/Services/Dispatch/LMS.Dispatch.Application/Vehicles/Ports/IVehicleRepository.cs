using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Vehicles.Models;
using LMS.Dispatch.Domain.Vehicles;

namespace LMS.Dispatch.Application.Vehicles.Ports;

public interface IVehicleRepository
{
    Task<Vehicle?> FindByIdAsync(string vehicleId, CancellationToken cancellationToken);

    Task<Vehicle?> FindVisibleAsync(string vehicleId, PropertyScope scope, CancellationToken cancellationToken);

    Task<Vehicle?> FindByPlateAsync(string plate, CancellationToken cancellationToken);

    Task<bool> IsDriverAssignedAsync(string driverId, CancellationToken cancellationToken);

    Task<PageResult<Vehicle>> ListAsync(ListVehiclesQuery query, CancellationToken cancellationToken);

    Task<VehicleStatusCounts> CountByStatusAsync(PropertyScope scope, string? propertyId, CancellationToken cancellationToken);

    // 车牌（不区分大小写）或已指派司机重复时抛出 DuplicateRecordException。
    Task InsertAsync(Vehicle vehicle, ITransactionContext context, CancellationToken cancellationToken);
}
