using LMS.Booking.Domain.Trips;

namespace LMS.Tests.Trips;

public class TripRulesTests
{
    private static readonly VehicleCapacity Sedan = new(VehicleClass.LuxurySedan, MaxPassengers: 3, MaxLuggage: 2, PointsRequired: 3000);

    private static readonly DateTimeOffset Now = new(2026, 10, 10, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AcceptsPassengersAndLuggageWithinCapacity()
    {
        var issues = TripRules.Validate(VehicleClass.LuxurySedan, 3, 2, Now.AddHours(1), enforceLeadTime: true, Now, 15, Sedan);

        Assert.Empty(issues);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void RejectsPassengersOutsideVehicleCapacity(int passengers)
    {
        var issues = TripRules.Validate(VehicleClass.LuxurySedan, passengers, 1, Now.AddHours(1), true, Now, 15, Sedan);

        Assert.Contains(issues, issue => issue.Field == "passengers");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void RejectsLuggageOutsideVehicleCapacity(int luggage)
    {
        var issues = TripRules.Validate(VehicleClass.LuxurySedan, 1, luggage, Now.AddHours(1), true, Now, 15, Sedan);

        Assert.Contains(issues, issue => issue.Field == "luggage");
    }

    [Fact]
    public void RejectsScheduledPickupInsideLeadTime()
    {
        var issues = TripRules.Validate(VehicleClass.LuxurySedan, 1, 0, Now.AddMinutes(10), true, Now, 15, Sedan);

        Assert.Contains(issues, issue => issue.Field == "pickupAt");
    }

    [Fact]
    public void LeadTimeDoesNotApplyWhenExempt()
    {
        var issues = TripRules.Validate(VehicleClass.LuxurySedan, 1, 0, Now.AddMinutes(1), enforceLeadTime: false, Now, 15, Sedan);

        Assert.Empty(issues);
    }
}
