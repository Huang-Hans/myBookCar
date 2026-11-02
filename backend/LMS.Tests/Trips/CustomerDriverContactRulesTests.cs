using LMS.Booking.Application.SelfService.Services;
using LMS.Booking.Domain.Trips;

namespace LMS.Tests.Trips;

public sealed class CustomerDriverContactRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 23, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void UnassignedTripHasNoDriverContact()
    {
        Assert.False(CustomerDriverContactRules.CanContact(CreateTrip()));
    }

    [Fact]
    public void ActiveAssignedTripHasDriverContact()
    {
        Assert.True(CustomerDriverContactRules.CanContact(AssignedTrip()));
    }

    [Fact]
    public void TerminalTripDoesNotExposeDriverContact()
    {
        Assert.False(CustomerDriverContactRules.CanContact(AssignedTrip().AdvanceTo(TripStatus.Completed)));
        Assert.False(CustomerDriverContactRules.CanContact(AssignedTrip().Cancel(new TripCancellation("CR-001", "Guest request", "guest_1", Now, "PR-001"))));
    }

    private static Trip AssignedTrip() => CreateTrip().AssignDriver(
        new DriverAssignment("offer_1", "driver_1", "Driver One", "Vehicle One", Now));

    private static Trip CreateTrip() => Trip.Create(
        "TR-20261023-1",
        "COD",
        new TripGuest("guest_1", "Guest One", "GOLD", null, null, null),
        new TripRoute("location_1", "Pickup", "location_2", "Drop-off", false),
        Now.AddHours(2),
        PickupMode.Scheduled,
        ServiceType.OneWay,
        VehicleClass.LuxurySedan,
        1,
        0,
        null,
        0,
        new BookedBy("guest_1", "CUSTOMER"),
        "EN",
        Now);
}
