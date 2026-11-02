using LMS.BuildingBlocks.Core.Patching;
using LMS.BuildingBlocks.Http.Requests;
using LMS.Platform.Api.Contracts.Locations;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.Locations.Commands;
using LMS.Platform.Application.Locations.Models;
using LMS.Platform.Domain.Locations;

namespace LMS.Platform.Api.Mapping.Locations;

internal static class LocationRequestMapper
{
    private const int MaxLocationIds = 100;

    public static ListLocationsQuery ToListQuery(LocationListRequest request) =>
        new(request.ToListQuery(), request.IsActive, request.Region, request.CategoryCode, ReadLocationIds(request.LocationIds));

    private static IReadOnlyList<string>? ReadLocationIds(string? text) => text is null
        ? null
        : text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.Ordinal).Take(MaxLocationIds).ToList();

    public static RequestParseResult<CreateLocationCommand> ToCreateCommand(
        CreateLocationRequest request,
        string idempotencyKey,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var command = new CreateLocationCommand(
            errors.RequiredText("name", request.Name),
            errors.RequiredText("address", request.Address),
            ReadCoordinates(errors, request.Coordinates),
            errors.RequiredEnum<LocationRegion>("region", request.Region),
            errors.RequiredText("categoryCode", request.CategoryCode),
            errors.RequiredText("contactPerson", request.ContactPerson),
            errors.RequiredText("contactPhone", request.ContactPhone),
            errors.RequiredText("operatingHours", request.OperatingHours),
            ReadGeofence(errors, request.Geofence),
            errors.RequiredBool("isActive", request.IsActive),
            Optional<string?>.Of(request.Notes).PatchNullableText().Value,
            idempotencyKey,
            context);

        return errors.HasErrors
            ? RequestParseResult<CreateLocationCommand>.Fail(errors.ToFailure())
            : RequestParseResult<CreateLocationCommand>.Success(command);
    }

    public static RequestParseResult<UpdateLocationCommand> ToUpdateCommand(
        string locationId,
        long expectedVersion,
        PatchLocationRequest request,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var changes = ReadChanges(errors, request);
        if (changes.ChangedFields().Count == 0)
        {
            errors.Add(string.Empty, "At least one field is required");
        }

        return errors.HasErrors
            ? RequestParseResult<UpdateLocationCommand>.Fail(errors.ToFailure())
            : RequestParseResult<UpdateLocationCommand>.Success(new UpdateLocationCommand(locationId, expectedVersion, changes, context));
    }

    public static RequestParseResult<ReplaceLocationOrderCommand> ToOrderCommand(
        LocationRegion region,
        LocationOrderingRequest request,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var items = LocationOrderItemReader.Read(errors, request.Items);

        return errors.HasErrors
            ? RequestParseResult<ReplaceLocationOrderCommand>.Fail(errors.ToFailure())
            : RequestParseResult<ReplaceLocationOrderCommand>.Success(new ReplaceLocationOrderCommand(region, items, context));
    }

    private static LocationChanges ReadChanges(FieldErrorCollector errors, PatchLocationRequest request) =>
        new(
            errors.PatchText("name", request.Name),
            errors.PatchText("address", request.Address),
            ReadCoordinatesPatch(errors, request.Coordinates),
            errors.PatchEnum<LocationRegion>("region", request.Region),
            errors.PatchText("categoryCode", request.CategoryCode),
            errors.PatchText("contactPerson", request.ContactPerson),
            errors.PatchText("contactPhone", request.ContactPhone),
            errors.PatchText("operatingHours", request.OperatingHours),
            ReadGeofencePatch(errors, request.Geofence),
            errors.PatchBool("isActive", request.IsActive),
            request.Notes.PatchNullableText());

    private static Coordinates ReadCoordinates(FieldErrorCollector errors, CoordinatesRequest? value)
    {
        if (value is null)
        {
            errors.Add("coordinates", "Is required");
            return new Coordinates(0, 0);
        }

        return new Coordinates(
            errors.RequiredNumber("coordinates.latitude", value.Latitude),
            errors.RequiredNumber("coordinates.longitude", value.Longitude));
    }

    private static Geofence ReadGeofence(FieldErrorCollector errors, GeofenceRequest? value)
    {
        if (value is null)
        {
            errors.Add("geofence", "Is required");
            return new Geofence(false, 0);
        }

        return new Geofence(
            errors.RequiredBool("geofence.isEnabled", value.IsEnabled),
            errors.RequiredNumber("geofence.radiusMeters", value.RadiusMeters));
    }

    private static CoordinatesChanges? ReadCoordinatesPatch(FieldErrorCollector errors, Optional<CoordinatesPatchRequest?> value)
    {
        if (!value.IsSet)
        {
            return null;
        }

        if (value.Value is null)
        {
            errors.Add("coordinates", "Must not be null");
            return null;
        }

        return new CoordinatesChanges(
            errors.PatchNumber("coordinates.latitude", value.Value.Latitude),
            errors.PatchNumber("coordinates.longitude", value.Value.Longitude));
    }

    private static GeofenceChanges? ReadGeofencePatch(FieldErrorCollector errors, Optional<GeofencePatchRequest?> value)
    {
        if (!value.IsSet)
        {
            return null;
        }

        if (value.Value is null)
        {
            errors.Add("geofence", "Must not be null");
            return null;
        }

        return new GeofenceChanges(
            errors.PatchBool("geofence.isEnabled", value.Value.IsEnabled),
            errors.PatchNumber("geofence.radiusMeters", value.Value.RadiusMeters));
    }
}
