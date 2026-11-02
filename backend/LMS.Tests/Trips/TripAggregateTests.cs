using LMS.Booking.Domain.Trips;

namespace LMS.Tests.Trips;

public class TripAggregateTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 8, 0, 0, TimeSpan.Zero);

    private static Trip CreateTrip(bool waitlisted = false) => Trip.Create(
        "TR-20261010-001",
        "COD",
        new TripGuest("guest_1", "James Liu", "DIAMOND", "2201", "MC00893521", "+853 6***1538"),
        new TripRoute("location_1", "Nuwa Lobby", "location_2", "Airport", IsCrossBorder: false),
        Now.AddHours(2),
        PickupMode.Scheduled,
        ServiceType.AirportDropoff,
        VehicleClass.LuxuryMpv,
        passengers: 2,
        luggage: 2,
        specialRequests: null,
        waitlisted,
        pointsRequired: 4500,
        bookedBy: new BookedBy("user_1", "PROPERTY_CS_STAFF"),
        confirmationLanguage: "EN",
        now: Now);

    [Fact]
    public void CreateConfirmsImmediatelyAndReservesPoints()
    {
        var trip = CreateTrip();

        Assert.Equal(TripStatus.Confirmed, trip.Status);
        Assert.Equal(PointsState.Reserved, trip.Points.State);
        Assert.Equal(4500, trip.Points.Required);
        Assert.False(trip.IsTerminal);
    }

    [Fact]
    public void WaitlistedCreateStartsAsPending()
    {
        var trip = CreateTrip(waitlisted: true);

        Assert.Equal(TripStatus.Pending, trip.Status);
    }

    [Fact]
    public void CancelMarksReturnedPoints()
    {
        var trip = CreateTrip().Cancel(new TripCancellation("CR-001", "Guest request", "user_1", Now, "PR-001"));

        Assert.Equal(TripStatus.Cancelled, trip.Status);
        Assert.Equal(PointsState.Returned, trip.Points.State);
        Assert.True(trip.IsTerminal);
        Assert.Equal("CR-001", trip.Cancellation!.ReasonCode);
    }

    [Fact]
    public void UpdateReplacesRouteAndTime()
    {
        var route = new TripRoute("location_3", "Morpheus Lobby", "location_2", "Airport", false);
        var updated = CreateTrip().Update(route, Now.AddHours(3), PickupMode.Scheduled, ServiceType.InterProperty,
            VehicleClass.LuxurySedan, 1, 1, "Child seat", "ZH", 3000);

        Assert.Equal("location_3", updated.Route.PickupLocationId);
        Assert.Equal(Now.AddHours(3), updated.PickupAt);
        Assert.Equal(VehicleClass.LuxurySedan, updated.VehicleClass);
        Assert.Equal(3000, updated.Points.Required);
        Assert.Equal(TripStatus.Confirmed, updated.Status);
    }

    [Fact]
    public void CompletingRedeemsPoints()
    {
        var trip = CreateTrip()
            .AdvanceTo(TripStatus.DriverAssigned)
            .AdvanceTo(TripStatus.DriverEnRoute)
            .AdvanceTo(TripStatus.ArrivedAtPickup)
            .AdvanceTo(TripStatus.InProgress)
            .AdvanceTo(TripStatus.Completed);

        Assert.Equal(TripStatus.Completed, trip.Status);
        Assert.Equal(PointsState.Redeemed, trip.Points.State);
    }
}
