using LMS.Dispatch.Domain.Vehicles;
using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Directory;
using LMS.Dispatch.Application.Offers.Commands;
using LMS.Dispatch.Application.Offers.Ports;
using LMS.Dispatch.Domain.Drivers;
using LMS.Dispatch.Domain.Offers;
using LMS.Dispatch.Application.Shifts.Services;

namespace LMS.Dispatch.Application.Offers.Services;

// 创建邀请前的校验链。每个方法返回 null 表示通过，否则给出应直接返回的失败。
// 候选资格与候选查询共用 DispatchCandidateRules；行程与司机各自只能有一张占用中的邀请，最终由存储的唯一约束裁决并发。
internal sealed class OfferCreationGuard(IDriverOfferRepository offers, OfferExpirationService expiration, PickupShiftQuery shifts)
{
    public OperationResult<DriverOffer>? CheckTrip(CreateDriverOfferCommand command, BookingResult<AssignableTrip> trip)
    {
        if (!trip.IsSuccess)
        {
            return FailureOf(trip.Outcome);
        }

        if (!command.Context.Scope.Allows(trip.Value!.PropertyId))
        {
            return OperationResult<DriverOffer>.Failed(OperationOutcome.Forbidden);
        }

        return trip.Value.IsAssignable ? null : OperationResult<DriverOffer>.Failed(OperationOutcome.StateConflict);
    }

    public OperationResult<DriverOffer>? CheckDriver(Driver driver, AssignableTrip trip) =>
        CheckVehicle(driver, trip) is { } issue ? OperationResult<DriverOffer>.Rejected([issue]) : null;

    public async Task<OperationResult<DriverOffer>?> CheckShiftAsync(Driver driver, AssignableTrip trip, CancellationToken cancellationToken) =>
        await shifts.IsOnDutyAsync(driver.DriverId, trip.PickupAt, trip.PropertyId, cancellationToken)
            ? null
            : OperationResult<DriverOffer>.Rejected([new FieldIssue("driverId", "Driver must be on duty for the shift covering the pickup time")]);

    private static FieldIssue? CheckVehicle(Driver driver, AssignableTrip trip) =>
        DispatchCandidateRules.Check(driver, trip.PropertyId) ??
        (driver.AssignedVehicleGrade is { } grade && grade == BookingVehicleGrades.FromClass(trip.VehicleClass)
            ? null : new FieldIssue("driverId", "Driver must have an enabled vehicle of the required class in the trip property"));

    // 先关闭已过期的旧轮次，再核对调度员看到的轮次，最后确认行程与司机都没有占用中的邀请。
    public async Task<OperationResult<DriverOffer>?> CheckOfferStateAsync(CreateDriverOfferCommand command, CancellationToken cancellationToken)
    {
        await expiration.ExpireStaleAsync(
            [.. await offers.ListOpenForTripAsync(command.TripId, cancellationToken), .. await offers.ListHeldByDriverAsync(command.DriverId, cancellationToken)],
            cancellationToken);

        var latest = await offers.FindLatestForTripAsync(command.TripId, cancellationToken);
        if (command.ExpectedRound > 0 && command.ExpectedRound != (latest?.Round ?? 0))
        {
            return OperationResult<DriverOffer>.Failed(OperationOutcome.VersionConflict);
        }

        return await HasHoldAsync(command, cancellationToken) ? OperationResult<DriverOffer>.Failed(OperationOutcome.StateConflict) : null;
    }

    private async Task<bool> HasHoldAsync(CreateDriverOfferCommand command, CancellationToken cancellationToken) =>
        (await offers.ListOpenForTripAsync(command.TripId, cancellationToken)).Count > 0
        || (await offers.ListHeldByDriverAsync(command.DriverId, cancellationToken)).Count > 0;

    private static OperationResult<DriverOffer> FailureOf(BookingOutcome outcome) => outcome switch
    {
        BookingOutcome.NotFound => OperationResult<DriverOffer>.Failed(OperationOutcome.NotFound),
        BookingOutcome.Forbidden => OperationResult<DriverOffer>.Failed(OperationOutcome.Forbidden),
        BookingOutcome.Conflict => OperationResult<DriverOffer>.Failed(OperationOutcome.StateConflict),
        _ => OperationResult<DriverOffer>.Failed(OperationOutcome.SourceUnavailable)
    };
}
