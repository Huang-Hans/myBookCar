using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Configuration.Commands;
using LMS.Platform.Application.Configuration.Models;

namespace LMS.Platform.Application.Configuration.Handlers;

public interface IUpdatePropertyHandler
{
    Task<OperationResult<ConfiguredProperty>> HandleAsync(UpdatePropertyCommand command, CancellationToken cancellationToken);
}
