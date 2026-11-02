using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Vehicles.Commands;
using LMS.Dispatch.Application.Vehicles.Models;

namespace LMS.Dispatch.Application.Vehicles.Handlers;

public interface ICreateVehicleHandler
{
    Task<OperationResult<VehicleCard>> HandleAsync(CreateVehicleCommand command, CancellationToken cancellationToken);
}
