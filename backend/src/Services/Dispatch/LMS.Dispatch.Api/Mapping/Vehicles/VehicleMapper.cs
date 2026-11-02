using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Requests;
using LMS.Dispatch.Api.Contracts.Vehicles;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Vehicles.Commands;
using LMS.Dispatch.Application.Vehicles.Models;
using LMS.Dispatch.Domain.Vehicles;

namespace LMS.Dispatch.Api.Mapping.Vehicles;

internal static class VehicleMapper
{
    public static ListVehiclesQuery ToListQuery(VehicleListRequest request, PropertyScope scope) =>
        new(request.ToListQuery(), scope, request.PropertyId, request.Grade, request.Status);

    public static VehicleSummaryQuery ToSummaryQuery(VehicleSummaryRequest request, PropertyScope scope) =>
        new(scope, request.PropertyId);

    public static RequestParseResult<CreateVehicleCommand> ToCreateCommand(
        CreateVehicleRequest request,
        string idempotencyKey,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var command = new CreateVehicleCommand(
            errors.RequiredText("plate", request.Plate),
            errors.RequiredText("model", request.Model),
            errors.RequiredEnum<VehicleGrade>("grade", request.Grade),
            errors.RequiredText("propertyId", request.PropertyId),
            ReadSeats(errors, request.Seats),
            ReadMileage(errors, request.MileageKm),
            request.Status is null ? VehicleStatus.Available : errors.RequiredEnum<VehicleStatus>("status", request.Status),
            ReadDriverId(errors, request.DriverId),
            idempotencyKey,
            context);

        return errors.HasErrors
            ? RequestParseResult<CreateVehicleCommand>.Fail(errors.ToFailure())
            : RequestParseResult<CreateVehicleCommand>.Success(command);
    }

    public static VehicleView ToView(VehicleCard card) =>
        new(
            card.Vehicle.VehicleId,
            card.Vehicle.Plate,
            card.Vehicle.Model,
            WireEnum.ToWire(card.Vehicle.Grade),
            card.Vehicle.PropertyId,
            card.Vehicle.Seats,
            card.Vehicle.MileageKm,
            WireEnum.ToWire(card.Vehicle.Status),
            card.Vehicle.DriverId,
            card.DriverName,
            card.Vehicle.Version);

    public static VehicleSummaryView ToSummary(VehicleStatusCounts counts) =>
        new(counts.Available, counts.OnTrip, counts.Maintenance, counts.OutOfService);

    private static int ReadSeats(FieldErrorCollector errors, int? seats)
    {
        var value = errors.RequiredInt("seats", seats);
        if (seats is < 1)
        {
            errors.Add("seats", "Must be a positive integer");
        }

        return value;
    }

    private static int ReadMileage(FieldErrorCollector errors, int? mileageKm)
    {
        var value = errors.RequiredInt("mileageKm", mileageKm);
        if (mileageKm is < 0)
        {
            errors.Add("mileageKm", "Must be zero or greater");
        }

        return value;
    }

    private static string? ReadDriverId(FieldErrorCollector errors, string? driverId)
    {
        if (driverId is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(driverId))
        {
            errors.Add("driverId", "Must not be blank");
            return null;
        }

        return driverId.Trim();
    }
}
