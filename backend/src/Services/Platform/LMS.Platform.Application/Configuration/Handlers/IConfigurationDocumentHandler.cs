using LMS.BuildingBlocks.Core.Operations;

namespace LMS.Platform.Application.Configuration.Handlers;

public interface IConfigurationDocumentHandler<TModel>
{
    Task<OperationResult<TModel>> GetAsync(CancellationToken cancellationToken);
}
