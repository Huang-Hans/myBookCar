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

internal sealed class MongoVehicleIssueRepository(IMongoDatabase database) : IVehicleIssueRepository
{
    private static readonly IReadOnlyDictionary<string, string> SortFields = new Dictionary<string, string>
    {
        ["issueId"] = "_id",
        ["reportedAt"] = "reportedAt"
    };

    private IMongoCollection<VehicleIssueDocument> Collection =>
        database.GetCollection<VehicleIssueDocument>(DispatchCollections.VehicleIssues);

    public async Task<VehicleIssue?> FindByIdAsync(string issueId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(candidate => candidate.IssueId == issueId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<VehicleIssue?> FindForDriverAsync(string issueId, string driverId, CancellationToken cancellationToken)
    {
        var document = await Collection
            .Find(candidate => candidate.IssueId == issueId && candidate.DriverId == driverId)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<PageResult<VehicleIssue>> ListForDriverAsync(ListVehicleIssuesQuery query, CancellationToken cancellationToken)
    {
        var filter = Builders<VehicleIssueDocument>.Filter;
        var combined = new MongoFilterBuilder<VehicleIssueDocument>(filter.Eq(candidate => candidate.DriverId, query.DriverId))
            .When(query.Status is not null, () => filter.Eq(candidate => candidate.Status, WireEnum.ToWire(query.Status!.Value)))
            .Build();
        var page = await MongoPagedQuery.FindPageAsync(
            Collection, combined, MongoSortFactory.Create<VehicleIssueDocument>(query.List.Sort, SortFields), query.List, cancellationToken);

        return new PageResult<VehicleIssue>(page.Items.Select(document => document.ToDomain()).ToList(), page.TotalItems);
    }

    public Task InsertAsync(VehicleIssue issue, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(Collection, MongoTransactionContext.RequireSession(context), VehicleIssueDocument.FromDomain(issue), cancellationToken);
}
