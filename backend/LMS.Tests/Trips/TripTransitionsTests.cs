using LMS.Booking.Domain.Trips;

namespace LMS.Tests.Trips;

public class TripTransitionsTests
{
    [Theory]
    [InlineData(TripStatus.Pending, TripStatus.Confirmed)]
    [InlineData(TripStatus.Pending, TripStatus.Cancelled)]
    [InlineData(TripStatus.Confirmed, TripStatus.DriverAssigned)]
    [InlineData(TripStatus.Confirmed, TripStatus.Cancelled)]
    [InlineData(TripStatus.DriverAssigned, TripStatus.DriverEnRoute)]
    [InlineData(TripStatus.DriverAssigned, TripStatus.Cancelled)]
    [InlineData(TripStatus.DriverEnRoute, TripStatus.ArrivedAtPickup)]
    [InlineData(TripStatus.DriverEnRoute, TripStatus.Cancelled)]
    [InlineData(TripStatus.ArrivedAtPickup, TripStatus.InProgress)]
    [InlineData(TripStatus.ArrivedAtPickup, TripStatus.Cancelled)]
    [InlineData(TripStatus.InProgress, TripStatus.Completed)]
    public void AllowsStateMachineTransitions(TripStatus from, TripStatus to) =>
        Assert.True(TripTransitions.CanTransition(from, to));

    [Theory]
    [InlineData(TripStatus.Completed, TripStatus.Cancelled)]
    [InlineData(TripStatus.Cancelled, TripStatus.Confirmed)]
    [InlineData(TripStatus.InProgress, TripStatus.Cancelled)]
    [InlineData(TripStatus.Pending, TripStatus.Completed)]
    [InlineData(TripStatus.Confirmed, TripStatus.InProgress)]
    public void RejectsIllegalTransitions(TripStatus from, TripStatus to) =>
        Assert.False(TripTransitions.CanTransition(from, to));

    [Theory]
    [InlineData(TripStatus.Pending)]
    [InlineData(TripStatus.Confirmed)]
    [InlineData(TripStatus.DriverAssigned)]
    [InlineData(TripStatus.DriverEnRoute)]
    [InlineData(TripStatus.ArrivedAtPickup)]
    public void AllowsCancelBeforeInProgress(TripStatus status) =>
        Assert.True(TripTransitions.CanCancel(status));

    [Theory]
    [InlineData(TripStatus.InProgress)]
    [InlineData(TripStatus.Completed)]
    [InlineData(TripStatus.Cancelled)]
    public void RejectsCancelDuringAndAfterTrip(TripStatus status) =>
        Assert.False(TripTransitions.CanCancel(status));

    [Fact]
    public void ModifyIsAllowedOnlyBeforeDriverAssignment()
    {
        Assert.True(TripTransitions.CanModify(TripStatus.Pending));
        Assert.True(TripTransitions.CanModify(TripStatus.Confirmed));
        Assert.True(TripTransitions.CanModify(TripStatus.DriverAssigned));
        Assert.False(TripTransitions.CanModify(TripStatus.DriverEnRoute));
        Assert.False(TripTransitions.CanModify(TripStatus.Completed));
    }
}
