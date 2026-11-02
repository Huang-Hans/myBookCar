using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Dispatch.Application.Vehicles.Models;

namespace LMS.Dispatch.Application.Vehicles.Handlers;

public interface IVehicleQueryHandler
{
    Task<OperationResult<PageResult<VehicleCard>>> ListAsync(ListVehiclesQuery query, CancellationToken cancellationToken);

    Task<OperationResult<VehicleStatusCounts>> SummarizeAsync(VehicleSummaryQuery query, CancellationToken cancellationToken);

    Task<OperationResult<VehicleCard>> GetAsync(string vehicleId, VehicleSummaryQuery scope, CancellationToken cancellationToken);
}
