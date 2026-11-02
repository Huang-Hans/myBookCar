using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Reports.Models;
using LMS.Dispatch.Application.Reports.Ports;
using LMS.Dispatch.Domain.Reports;
using LMS.Dispatch.Infrastructure.Documents;
using LMS.Dispatch.Infrastructure.Persistence;
using LMS.Dispatch.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Dispatch.Infrastructure.Repositories;

internal sealed class MongoDriverIncidentRepository(IMongoDatabase database) : IDriverIncidentRepository
{
    private static readonly IReadOnlyDictionary<string, string> SortFields = new Dictionary<string, string>
    {
        ["incidentId"] = "_id",
        ["reportedAt"] = "reportedAt",
        ["occurredAt"] = "occurredAt"
    };

    private IMongoCollection<DriverIncidentDocument> Collection =>
        database.GetCollection<DriverIncidentDocument>(DispatchCollections.DriverIncidents);

    public async Task<DriverIncident?> FindByIdAsync(string incidentId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(candidate => candidate.IncidentId == incidentId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<DriverIncident?> FindForDriverAsync(string incidentId, string driverId, CancellationToken cancellationToken)
    {
        var document = await Collection
            .Find(candidate => candidate.IncidentId == incidentId && candidate.DriverId == driverId)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<PageResult<DriverIncident>> ListForDriverAsync(ListIncidentsQuery query, CancellationToken cancellationToken)
    {
        var filter = Builders<DriverIncidentDocument>.Filter;
        var combined = new MongoFilterBuilder<DriverIncidentDocument>(filter.Eq(candidate => candidate.DriverId, query.DriverId))
            .When(query.Type is not null, () => filter.Eq(candidate => candidate.Type, WireEnum.ToWire(query.Type!.Value)))
            .When(query.Status is not null, () => filter.Eq(candidate => candidate.Status, WireEnum.ToWire(query.Status!.Value)))
            .Build();
        var page = await MongoPagedQuery.FindPageAsync(
            Collection, combined, MongoSortFactory.Create<DriverIncidentDocument>(query.List.Sort, SortFields), query.List, cancellationToken);

        return new PageResult<DriverIncident>(page.Items.Select(document => document.ToDomain()).ToList(), page.TotalItems);
    }

    public Task InsertAsync(DriverIncident incident, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(Collection, MongoTransactionContext.RequireSession(context), DriverIncidentDocument.FromDomain(incident), cancellationToken);
}
