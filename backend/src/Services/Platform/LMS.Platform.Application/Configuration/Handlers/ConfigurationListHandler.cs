using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Platform.Application.Configuration.Definitions;
using LMS.Platform.Application.Configuration.Ports;
using LMS.Platform.Application.Configuration.Services;

namespace LMS.Platform.Application.Configuration.Handlers;

internal sealed class ConfigurationListHandler<TItem>(
    IConfigurationListSource<TItem> source,
    ConfigurationListDefinition<TItem> definition) : IConfigurationListHandler<TItem>
{
    public async Task<OperationResult<PageResult<TItem>>> ListAsync(
        ListQuery query,
        Func<TItem, bool>? filter,
        CancellationToken cancellationToken)
    {
        var lookup = await source.LoadAsync(cancellationToken);
        if (!lookup.IsAvailable)
        {
            return OperationResult<PageResult<TItem>>.Failed(OperationOutcome.SourceUnavailable);
        }

        return OperationResult<PageResult<TItem>>.Succeeded(
            ConfigurationListEngine.Page(lookup.Value!, definition, query, filter));
    }

    public async Task<OperationResult<IReadOnlyList<TItem>>> ListAllAsync(
        Func<TItem, bool>? filter,
        CancellationToken cancellationToken)
    {
        var lookup = await source.LoadAsync(cancellationToken);
        if (!lookup.IsAvailable)
        {
            return OperationResult<IReadOnlyList<TItem>>.Failed(OperationOutcome.SourceUnavailable);
        }

        IReadOnlyList<TItem> items = lookup.Value!.Where(item => filter?.Invoke(item) ?? true).ToList();
        return OperationResult<IReadOnlyList<TItem>>.Succeeded(items);
    }

    public async Task<OperationResult<TItem>> FindAsync(string key, Func<TItem, bool>? filter, CancellationToken cancellationToken)
    {
        var lookup = await source.LoadAsync(cancellationToken);
        if (!lookup.IsAvailable)
        {
            return OperationResult<TItem>.Failed(OperationOutcome.SourceUnavailable);
        }

        var match = lookup.Value!.FirstOrDefault(item =>
            string.Equals(definition.KeyOf(item), key, StringComparison.Ordinal) && (filter?.Invoke(item) ?? true));

        return match is null ? OperationResult<TItem>.Failed(OperationOutcome.NotFound) : OperationResult<TItem>.Succeeded(match);
    }
}
