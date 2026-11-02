using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Booking.Application.Auditing.Ports;
using LMS.Booking.Application.Qualifications.Models;
using LMS.Booking.Domain.Auditing;
using LMS.Booking.Infrastructure.Documents;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Driver;

namespace LMS.Booking.Infrastructure.Repositories;

internal sealed class MongoQualificationAuditReader(IMongoDatabase database) : IQualificationAuditReader
{
    private static readonly string[] SearchFields =
        ["actor", "operation", "resourceId", "propertyId", "qualification.guestId", "qualification.justification", "qualification.status"];

    private static readonly IReadOnlyDictionary<string, string> SortFields = new Dictionary<string, string>
    {
        ["auditId"] = "_id",
        ["actor"] = "actor",
        ["occurredAt"] = "occurredAt",
        ["qualification.guestId"] = "qualification.guestId",
        ["qualification.justification"] = "qualification.justification",
        ["qualification.status"] = "qualification.status"
    };

    private IMongoCollection<AuditRecordDocument> Collection =>
        database.GetCollection<AuditRecordDocument>(BookingCollections.AuditRecords);

    public async Task<PageResult<AuditRecord>> ListAsync(ListQualificationAuditsQuery query, CancellationToken cancellationToken)
    {
        var page = await MongoPagedQuery.FindPageAsync(
            Collection,
            BuildFilter(query),
            MongoSortFactory.Create<AuditRecordDocument>(query.List.Sort, SortFields),
            query.List,
            cancellationToken);

        return new PageResult<AuditRecord>(page.Items.Select(document => document.ToDomain()).ToList(), page.TotalItems);
    }

    // 只返回带资格上下文的记录；非全物业调用者只看到自己物业的记录。
    private static FilterDefinition<AuditRecordDocument> BuildFilter(ListQualificationAuditsQuery query)
    {
        var filter = Builders<AuditRecordDocument>.Filter;
        var propertyId = query.PropertyId ?? (query.Scope.IsAllProperties ? null : query.Scope.PropertyId);

        return new MongoFilterBuilder<AuditRecordDocument>(filter.Ne(candidate => candidate.Qualification, null))
            .When(propertyId is not null, () => filter.Eq(candidate => candidate.PropertyId, propertyId))
            .When(query.List.HasSearch, () => MongoTextSearch.Contains<AuditRecordDocument>(SearchFields, query.List.Search))
            .Build();
    }
}
