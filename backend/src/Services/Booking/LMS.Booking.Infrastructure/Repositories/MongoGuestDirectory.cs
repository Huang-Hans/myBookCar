using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Booking.Application.Guests.Ports;
using LMS.Booking.Domain.Guests;
using LMS.Booking.Infrastructure.Documents;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Driver;

namespace LMS.Booking.Infrastructure.Repositories;

// IGuestDirectory 的一期实现：读 lms_booking.guests 快照（reference 数据源）。
// 检索匹配 姓名/会员号/房号/电话（包含、忽略大小写）；物业范围之外的宾客不可见。
internal sealed class MongoGuestDirectory(IMongoDatabase database) : IGuestDirectory
{
    private static readonly string[] SearchFields = ["name", "memberId", "roomNumber", "phone"];

    private static readonly IReadOnlyDictionary<string, string> SortFields = new Dictionary<string, string>
    {
        ["guestId"] = "_id",
        ["name"] = "name",
        ["memberId"] = "memberId",
        ["roomNumber"] = "roomNumber",
        ["tierCode"] = "tierCode"
    };

    private IMongoCollection<GuestDocument> Collection =>
        database.GetCollection<GuestDocument>(BookingCollections.Guests);

    public async Task<PageResult<GuestProfile>> SearchAsync(
        ListQuery list,
        string? tierCode,
        PropertyScope scope,
        CancellationToken cancellationToken)
    {
        var filter = Builders<GuestDocument>.Filter;
        var combined = new MongoFilterBuilder<GuestDocument>(VisibleTo(scope))
            .When(tierCode is not null, () => filter.Eq(candidate => candidate.TierCode, tierCode!))
            .When(list.HasSearch, () => MongoTextSearch.Contains<GuestDocument>(SearchFields, list.Search))
            .Build();
        var page = await MongoPagedQuery.FindPageAsync(
            Collection, combined, MongoSortFactory.Create<GuestDocument>(list.Sort, SortFields), list, cancellationToken);

        return new PageResult<GuestProfile>(page.Items.Select(document => document.ToDomain()).ToList(), page.TotalItems);
    }

    public async Task<GuestProfile?> FindAsync(string guestId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(candidate => candidate.GuestId == guestId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    // 导入按 member 规范化值幂等：同会员号重复导入覆盖既有快照（_id 保留首次分配的 guestId）。
    // 本地登录凭据（password）不来自上游快照：覆盖时保留既有哈希，快照里没有密码不会把它清空。
    public async Task UpsertAsync(GuestProfile guest, CancellationToken cancellationToken)
    {
        var normalized = guest.MemberIdNormalized;
        var existing = await Collection.Find(candidate => candidate.MemberIdNormalized == normalized).FirstOrDefaultAsync(cancellationToken);
        var document = GuestDocument.FromDomain(existing is null ? guest : guest with { GuestId = existing.GuestId });
        document.Password = existing?.Password;
        var options = new ReplaceOptions { IsUpsert = true };

        await Collection.ReplaceOneAsync(candidate => candidate.MemberIdNormalized == normalized, document, options, cancellationToken);
    }

    private static FilterDefinition<GuestDocument> VisibleTo(PropertyScope scope) =>
        scope.IsAllProperties
            ? FilterDefinition<GuestDocument>.Empty
            : Builders<GuestDocument>.Filter.Eq(candidate => candidate.PropertyId, scope.PropertyId);
}
