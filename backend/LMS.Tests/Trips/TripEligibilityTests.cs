using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Catalog.Ports;
using LMS.Booking.Application.Console.Options;
using LMS.Booking.Application.Directory;
using LMS.Booking.Application.Guests.Ports;
using LMS.Booking.Application.Quotas.Models;
using LMS.Booking.Application.Quotas.Ports;
using LMS.Booking.Application.Trips.Models;
using LMS.Booking.Application.Trips.Ports;
using LMS.Booking.Application.Trips.Services;
using LMS.Booking.Domain.Guests;
using LMS.Booking.Domain.Quotas;
using LMS.Booking.Domain.Trips;
using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using Microsoft.Extensions.Options;

namespace LMS.Tests.Trips;

// 建单校验链（TripEligibilityService）的行为验证：校验顺序即错误优先级、422/40001/404/503 分类、
// 资格不满足时 RequiresManagerReview 标记（供登记待复核档案）、候配额受理为 WAITLIST。
public class TripEligibilityTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 8, 0, 0, TimeSpan.Zero);

    private readonly FakeGuestDirectory guests = new()
    {
        Guest = new GuestProfile(
            "guest_1", "MC00893521", "James Liu", "DIAMOND", "2201", "COD", null, null,
            62_000, "+853 6123 4829", InHouse: true, PlayVerified: true, PlayWindowDays: 90,
            null, null, null, null),
    };

    private readonly FakePlatformCatalog catalog = new()
    {
        Locations =
        {
            ["loc_pickup"] = new CatalogLocation("loc_pickup", "Nuwa Lobby", "COTAI", "LC-001", IsActive: true),
            ["loc_dropoff"] = new CatalogLocation("loc_dropoff", "Macau Airport", "COTAI", "LC-001", IsActive: true),
            ["loc_hk"] = new CatalogLocation("loc_hk", "Hong Kong Kowloon", "HONG_KONG", "LC-002", IsActive: true),
        },
    };

    private readonly FakeLoyaltyGateway loyalty = new()
    {
        Qualification = new LoyaltyQualification("DIAMOND", PlayVerified: true, PlayWindowDays: 90, PointsBalance: 62_000),
    };

    private readonly FakeQuotaRepository quotas = new();
    private readonly FakeTripRepository trips = new();
    private readonly FakePlatformDirectory platform = new();

    private TripEligibilityService CreateService() => new(
        guests, catalog, platform, loyalty, quotas, trips,
        Options.Create(new ConsoleOptions()),
        new FixedTimeProvider(Now));

    private static TripEvaluationRequest Request(
        VehicleClass vehicleClass = VehicleClass.LuxurySedan,
        string dropoffLocationId = "loc_dropoff",
        DateTimeOffset? pickupAt = null,
        int passengers = 2,
        int luggage = 1,
        PickupMode pickupMode = PickupMode.Scheduled) => new(
            "guest_1", "COD", "loc_pickup", dropoffLocationId,
            pickupAt ?? Now.AddHours(2), pickupMode, vehicleClass, passengers, luggage);

    [Fact]
    public async Task UnknownGuestAnswersNotFound()
    {
        guests.Guest = null;

        var verdict = await CreateService().EvaluateAsync(Request(), CancellationToken.None);

        Assert.Equal(OperationOutcome.NotFound, verdict.Result.Outcome);
    }

    [Fact]
    public async Task UnknownLocationAnswersNotFound()
    {
        var verdict = await CreateService().EvaluateAsync(Request(dropoffLocationId: "loc_missing"), CancellationToken.None);

        Assert.Equal(OperationOutcome.NotFound, verdict.Result.Outcome);
    }

    [Fact]
    public async Task InactiveLocationIsAFieldError()
    {
        catalog.Locations["loc_pickup"] = catalog.Locations["loc_pickup"] with { IsActive = false };

        var verdict = await CreateService().EvaluateAsync(Request(), CancellationToken.None);

        Assert.Equal(OperationOutcome.ValidationFailed, verdict.Result.Outcome);
        Assert.Contains(verdict.Result.Issues, issue => issue.Field == "route.pickupLocationId");
    }

    [Fact]
    public async Task ActiveCustomerRestrictionIsRejectedWithoutManagerReview()
    {
        platform.Restrictions = [new CustomerRestrictionInfo("MC00893521", "Self-exclusion")];

        var verdict = await CreateService().EvaluateAsync(Request(), CancellationToken.None);

        Assert.Equal(OperationOutcome.BusinessRuleRejected, verdict.Result.Outcome);
        Assert.Contains(verdict.Result.Issues, issue => issue.Field == "guest");
        Assert.False(verdict.RequiresManagerReview);
    }

    [Fact]
    public async Task UnavailableBlacklistSourceAnswersServiceUnavailable()
    {
        platform.Unavailable = true;

        var verdict = await CreateService().EvaluateAsync(Request(), CancellationToken.None);

        Assert.Equal(OperationOutcome.SourceUnavailable, verdict.Result.Outcome);
    }

    [Fact]
    public async Task UnavailableLoyaltySourceAnswersServiceUnavailable()
    {
        loyalty.Unavailable = true;

        var verdict = await CreateService().EvaluateAsync(Request(), CancellationToken.None);

        Assert.Equal(OperationOutcome.SourceUnavailable, verdict.Result.Outcome);
    }

    [Fact]
    public async Task UnknownTierIsRejectedWithManagerReview()
    {
        loyalty.Qualification = new LoyaltyQualification("BRONZE", true, 90, 62_000);

        var verdict = await CreateService().EvaluateAsync(Request(), CancellationToken.None);

        Assert.Equal(OperationOutcome.BusinessRuleRejected, verdict.Result.Outcome);
        Assert.Contains(verdict.Result.Issues, issue => issue.Field == "qualification");
        Assert.True(verdict.RequiresManagerReview);
    }

    [Fact]
    public async Task VehicleClassOutsideEntitlementIsRejected()
    {
        loyalty.Qualification = new LoyaltyQualification("GOLD", true, 90, 62_000);

        var verdict = await CreateService().EvaluateAsync(Request(vehicleClass: VehicleClass.Maybach), CancellationToken.None);

        Assert.Equal(OperationOutcome.BusinessRuleRejected, verdict.Result.Outcome);
        Assert.Contains(verdict.Result.Issues, issue => issue.Field == "vehicleClass");
        Assert.True(verdict.RequiresManagerReview);
    }

    [Fact]
    public async Task UnverifiedPlayHistoryIsRejected()
    {
        loyalty.Qualification = new LoyaltyQualification("DIAMOND", PlayVerified: false, 90, 62_000);

        var verdict = await CreateService().EvaluateAsync(Request(), CancellationToken.None);

        Assert.Equal(OperationOutcome.BusinessRuleRejected, verdict.Result.Outcome);
        Assert.Contains(verdict.Result.Issues, issue => issue.Field == "qualification");
    }

    [Fact]
    public async Task InsufficientPointsAreRejected()
    {
        loyalty.Qualification = new LoyaltyQualification("DIAMOND", true, 90, PointsBalance: 1_000);

        var verdict = await CreateService().EvaluateAsync(Request(), CancellationToken.None);

        Assert.Equal(OperationOutcome.BusinessRuleRejected, verdict.Result.Outcome);
        Assert.Contains(verdict.Result.Issues, issue => issue.Field == "qualification");
    }

    [Fact]
    public async Task CapacityOverflowIsAFieldError()
    {
        var verdict = await CreateService().EvaluateAsync(Request(passengers: 9), CancellationToken.None);

        Assert.Equal(OperationOutcome.ValidationFailed, verdict.Result.Outcome);
        Assert.Contains(verdict.Result.Issues, issue => issue.Field == "passengers");
    }

    [Fact]
    public async Task ScheduledPickupInsideLeadTimeIsAFieldError()
    {
        var verdict = await CreateService().EvaluateAsync(Request(pickupAt: Now.AddMinutes(10)), CancellationToken.None);

        Assert.Equal(OperationOutcome.ValidationFailed, verdict.Result.Outcome);
        Assert.Contains(verdict.Result.Issues, issue => issue.Field == "pickupAt");
    }

    [Fact]
    public async Task AsapPickupBypassesLeadTime()
    {
        var verdict = await CreateService().EvaluateAsync(
            Request(pickupAt: Now.AddMinutes(5), pickupMode: PickupMode.Asap), CancellationToken.None);

        Assert.True(verdict.Result.IsSuccess);
    }

    [Fact]
    public async Task ExhaustedQuotaWithAutoRejectIsRejected()
    {
        quotas.Quota = new PropertyQuota("COD", 2, 10, 40, ExhaustionPolicy.AutoReject, null, null, null, 1);
        trips.DailyActiveCount = 2;

        var verdict = await CreateService().EvaluateAsync(Request(), CancellationToken.None);

        Assert.Equal(OperationOutcome.BusinessRuleRejected, verdict.Result.Outcome);
        Assert.Contains(verdict.Result.Issues, issue => issue.Field == "quota");
    }

    [Fact]
    public async Task ExhaustedQuotaWithWaitlistIsAcceptedAsPending()
    {
        quotas.Quota = new PropertyQuota("COD", 2, 10, 40, ExhaustionPolicy.Waitlist, null, null, null, 1);
        trips.DailyActiveCount = 2;

        var verdict = await CreateService().EvaluateAsync(Request(), CancellationToken.None);

        Assert.True(verdict.Result.IsSuccess);
        Assert.True(verdict.Result.Value!.Waitlisted);
    }

    [Fact]
    public async Task HappyPathResolvesRouteFromCatalog()
    {
        var verdict = await CreateService().EvaluateAsync(Request(), CancellationToken.None);

        Assert.True(verdict.Result.IsSuccess);
        var eligibility = verdict.Result.Value!;
        Assert.False(eligibility.Waitlisted);
        Assert.Equal("Nuwa Lobby", eligibility.Route.PickupName);
        Assert.Equal("Macau Airport", eligibility.Route.DropoffName);
        Assert.False(eligibility.Route.IsCrossBorder);
        Assert.Equal(3000, eligibility.Capacity.PointsRequired);
        Assert.NotNull(verdict.Guest);
    }

    [Fact]
    public async Task HongKongDropoffIsFlaggedCrossBorder()
    {
        var verdict = await CreateService().EvaluateAsync(Request(dropoffLocationId: "loc_hk"), CancellationToken.None);

        Assert.True(verdict.Result.IsSuccess);
        Assert.True(verdict.Result.Value!.Route.IsCrossBorder);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class FakeGuestDirectory : IGuestDirectory
    {
        public GuestProfile? Guest { get; set; }

        public Task<PageResult<GuestProfile>> SearchAsync(ListQuery list, string? tierCode, PropertyScope scope, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<GuestProfile?> FindAsync(string guestId, CancellationToken cancellationToken) =>
            Task.FromResult(Guest is not null && Guest.GuestId == guestId ? Guest : null);

        public Task UpsertAsync(GuestProfile guest, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakePlatformCatalog : IPlatformCatalog
    {
        public Dictionary<string, CatalogLocation> Locations { get; } = [];

        public Task<DirectoryResult<CatalogLocation>> GetLocationAsync(string locationId, CancellationToken cancellationToken) =>
            Task.FromResult(Locations.TryGetValue(locationId, out var location)
                ? DirectoryResult<CatalogLocation>.Success(location)
                : DirectoryResult<CatalogLocation>.Failed(DirectoryOutcome.NotFound));

        public Task<DirectoryResult<CatalogCancellationReason>> GetCancellationReasonAsync(string reasonCode, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<DirectoryResult<CatalogProperty>> GetPropertyAsync(string propertyId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakePlatformDirectory : IPlatformDirectory
    {
        public IReadOnlyList<CustomerRestrictionInfo> Restrictions { get; set; } = [];

        public bool Unavailable { get; set; }

        public Task<CallerLookup> ResolveCallerAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<PropertyLookup> ContainsPropertyAsync(string propertyId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<DirectoryResult<IReadOnlyList<CustomerRestrictionInfo>>> CheckCustomersAsync(
            IReadOnlyList<string> memberIds,
            CancellationToken cancellationToken) =>
            Task.FromResult(Unavailable
                ? DirectoryResult<IReadOnlyList<CustomerRestrictionInfo>>.Failed(DirectoryOutcome.Unavailable)
                : DirectoryResult<IReadOnlyList<CustomerRestrictionInfo>>.Success(Restrictions));

        public Task<DirectoryResult<RegisteredFile>> GetFileAsync(string fileId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<DirectoryResult<FileUploadInfo>> RegisterFileAsync(RegisterFileRequest request, string idempotencyKey, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeLoyaltyGateway : ILoyaltyGateway
    {
        public LoyaltyQualification? Qualification { get; set; }

        public bool Unavailable { get; set; }

        public Task<DirectoryResult<LoyaltyQualification>> GetQualificationAsync(string guestId, CancellationToken cancellationToken) =>
            Task.FromResult(Unavailable || Qualification is null
                ? DirectoryResult<LoyaltyQualification>.Failed(DirectoryOutcome.Unavailable)
                : DirectoryResult<LoyaltyQualification>.Success(Qualification));
    }

    private sealed class FakeQuotaRepository : IPropertyQuotaRepository
    {
        public PropertyQuota? Quota { get; set; }

        public Task<PropertyQuota?> FindByIdAsync(string propertyId, CancellationToken cancellationToken) =>
            Task.FromResult(Quota is not null && Quota.PropertyId == propertyId ? Quota : null);

        public Task<PropertyQuota?> FindVisibleAsync(string propertyId, PropertyScope scope, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<PropertyQuota?> FindVisibleAsync(string propertyId, PropertyScope scope, ITransactionContext context, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<PageResult<PropertyQuota>> ListAsync(ListPropertyQuotasQuery query, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<PropertyQuota>> ListVisibleAsync(PropertyScope scope, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> TryReplaceAsync(PropertyQuota updated, long expectedVersion, ITransactionContext context, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FakeTripRepository : ITripRepository
    {
        public long DailyActiveCount { get; set; }

        public Task<Trip?> FindByIdAsync(string tripId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<Trip?> FindVisibleAsync(string tripId, PropertyScope scope, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<Trip?> FindVisibleAsync(string tripId, PropertyScope scope, ITransactionContext context, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<PageResult<Trip>> ListAsync(ListTripsQuery query, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<Trip>> ListRecentByGuestAsync(string guestId, PropertyScope scope, int take, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task InsertAsync(Trip trip, ITransactionContext context, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> TryReplaceAsync(Trip updated, long expectedVersion, ITransactionContext context, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<string> NextReferenceAsync(DateOnly macauDate, ITransactionContext context, CancellationToken cancellationToken) =>
            Task.FromResult("TR-20261010-001");

        public Task<long> CountDailyActiveAsync(string propertyId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken) =>
            Task.FromResult(DailyActiveCount);

        public Task<IReadOnlyDictionary<TripStatus, long>> CountByStatusAsync(PropertyScope scope, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> TryAdvanceAsync(string tripId, TripStatus from, TripStatus to, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
