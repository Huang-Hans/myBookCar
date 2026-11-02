using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Inspections.Models;
using LMS.Dispatch.Application.Inspections.Ports;
using LMS.Dispatch.Domain.Inspections;
using LMS.Dispatch.Infrastructure.Documents;
using LMS.Dispatch.Infrastructure.Persistence;
using LMS.Dispatch.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Dispatch.Infrastructure.Repositories;

internal sealed class MongoDriverInspectionRepository(IMongoDatabase database) : IDriverInspectionRepository
{
    private static readonly IReadOnlyDictionary<string, string> SortFields = new Dictionary<string, string>
    {
        ["inspectionId"] = "_id",
        ["submittedAt"] = "submittedAt",
        ["phase"] = "phase"
    };

    private IMongoCollection<DriverInspectionDocument> Collection =>
        database.GetCollection<DriverInspectionDocument>(DispatchCollections.DriverInspections);

    public async Task<DriverInspection?> FindByIdAsync(string inspectionId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(candidate => candidate.InspectionId == inspectionId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<DriverInspection?> FindForDriverAsync(string inspectionId, string driverId, CancellationToken cancellationToken)
    {
        var document = await Collection
            .Find(candidate => candidate.InspectionId == inspectionId && candidate.DriverId == driverId)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<PageResult<DriverInspection>> ListForDriverAsync(ListInspectionsQuery query, CancellationToken cancellationToken)
    {
        var page = await MongoPagedQuery.FindPageAsync(
            Collection,
            BuildFilter(query),
            MongoSortFactory.Create<DriverInspectionDocument>(query.List.Sort, SortFields),
            query.List,
            cancellationToken);

        return new PageResult<DriverInspection>(page.Items.Select(document => document.ToDomain()).ToList(), page.TotalItems);
    }

    public Task InsertAsync(DriverInspection inspection, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(
            Collection, MongoTransactionContext.RequireSession(context), DriverInspectionDocument.FromDomain(inspection), cancellationToken);

    // 归属条件 driverId 永远是第一个条件；其余筛选与它取交集。
    private static FilterDefinition<DriverInspectionDocument> BuildFilter(ListInspectionsQuery query)
    {
        var filter = Builders<DriverInspectionDocument>.Filter;

        return new MongoFilterBuilder<DriverInspectionDocument>(filter.Eq(candidate => candidate.DriverId, query.DriverId))
            .When(query.Phase is not null, () => filter.Eq(candidate => candidate.Phase, WireEnum.ToWire(query.Phase!.Value)))
            .When(query.HasFailure is not null, () => filter.Eq(candidate => candidate.HasFailure, query.HasFailure!.Value))
            .When(query.From is not null, () => filter.Gte(candidate => candidate.SubmittedAt, UtcDateTimes.ToStored(query.From!.Value)))
            .When(query.To is not null, () => filter.Lt(candidate => candidate.SubmittedAt, UtcDateTimes.ToStored(query.To!.Value)))
            .Build();
    }
}
