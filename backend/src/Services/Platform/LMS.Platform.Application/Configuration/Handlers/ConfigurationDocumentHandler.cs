using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Configuration.Ports;

namespace LMS.Platform.Application.Configuration.Handlers;

internal sealed class ConfigurationDocumentHandler<TModel>(IConfigurationDocumentSource<TModel> source)
    : IConfigurationDocumentHandler<TModel>
{
    public async Task<OperationResult<TModel>> GetAsync(CancellationToken cancellationToken)
    {
        var lookup = await source.LoadAsync(cancellationToken);

        return lookup.IsAvailable
            ? OperationResult<TModel>.Succeeded(lookup.Value!)
            : OperationResult<TModel>.Failed(OperationOutcome.SourceUnavailable);
    }
}
