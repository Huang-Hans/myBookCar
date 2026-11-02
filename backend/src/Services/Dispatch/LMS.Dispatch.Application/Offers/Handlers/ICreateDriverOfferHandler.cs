using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Offers.Commands;
using LMS.Dispatch.Domain.Offers;

namespace LMS.Dispatch.Application.Offers.Handlers;

public interface ICreateDriverOfferHandler
{
    Task<OperationResult<DriverOffer>> HandleAsync(CreateDriverOfferCommand command, CancellationToken cancellationToken);
}
