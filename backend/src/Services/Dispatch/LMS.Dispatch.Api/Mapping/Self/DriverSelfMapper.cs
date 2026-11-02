using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Requests;
using LMS.Dispatch.Api.Contracts.Self;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Self.Commands;
using LMS.Dispatch.Domain.Drivers;

namespace LMS.Dispatch.Api.Mapping.Self;

internal static class DriverSelfMapper
{
    public static DriverSelfView ToView(Driver driver) => new(
        driver.DriverId,
        driver.Name,
        driver.PropertyId,
        driver.AssignedVehicle,
        driver.Languages,
        WireEnum.ToWire(driver.Status),
        driver.Availability is null ? null : WireEnum.ToWire(driver.Availability.Value),
        driver.AvailabilitySource,
        driver.SyncedAt?.UtcDateTime,
        new DriverCapabilitiesView(CanChangeAvailability(driver), DriverSelfRules.CanAcceptOffers(driver)),
        driver.Version);

    public static RequestParseResult<ChangeAvailabilityCommand> ToCommand(
        string driverId,
        ChangeAvailabilityRequest request,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var target = errors.RequiredEnum<DriverAvailability>("availability", request.Availability);

        return errors.HasErrors
            ? RequestParseResult<ChangeAvailabilityCommand>.Fail(errors.ToFailure())
            : RequestParseResult<ChangeAvailabilityCommand>.Success(new ChangeAvailabilityCommand(driverId, target, context));
    }

    private static bool CanChangeAvailability(Driver driver) =>
        driver.Status == DriverStatus.Active && driver.Availability != DriverAvailability.OnTrip;
}
