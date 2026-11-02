using LMS.BuildingBlocks.Http.Requests;
using LMS.Booking.Api.Contracts.TripLocations;
using LMS.Booking.Application.TripLocations.Commands;
using LMS.Booking.Application.TripLocations.Models;
using LMS.Booking.Domain.TripLocations;

namespace LMS.Booking.Api.Mapping.TripLocations;

internal static class TripLocationMapper
{
    // 单次上报的点数上限：离线补传时一次最多带这么多点，这是请求体积的安全边界，不是业务规则。
    public const int MaxPoints = 50;

    private const string RequiredCoordinateSystem = "GCJ02";

    public static RequestParseResult<RecordTripLocationsCommand> ToCommand(string tripId, string driverId, TripLocationUploadRequest request)
    {
        var errors = new FieldErrorCollector();
        if (request.CoordinateSystem != RequiredCoordinateSystem)
        {
            errors.Add("coordinateSystem", $"Must be {RequiredCoordinateSystem}");
        }

        var points = ReadPoints(errors, request.Points);

        return errors.HasErrors
            ? RequestParseResult<RecordTripLocationsCommand>.Fail(errors.ToFailure())
            : RequestParseResult<RecordTripLocationsCommand>.Success(new RecordTripLocationsCommand(tripId, driverId, points));
    }

    public static TripLocationUploadView ToUploadView(long acceptedSeq) => new(acceptedSeq);

    public static TripLocationSnapshotView ToView(TripLocationSnapshot snapshot) =>
        new(snapshot.TripId, snapshot.IsSharing, snapshot.Point is null ? null : ToView(snapshot.Point));

    public static TripTrackView ToView(TripTrack track) =>
        new(track.TripId, track.IsSharing, track.Sampled, track.Points.Select(ToView).ToList());

    public static TripLocationView ToView(LocationPoint point) =>
        new(point.Latitude, point.Longitude, point.HeadingDegrees, point.SpeedKmh, point.Seq, point.RecordedAt.UtcDateTime);

    private static List<LocationPoint> ReadPoints(FieldErrorCollector errors, List<TripLocationPointRequest?>? values)
    {
        if (values is null || values.Count is 0 or > MaxPoints)
        {
            errors.Add("points", $"Must contain 1 to {MaxPoints} points");
            return [];
        }

        var points = values.Select((value, index) => ReadPoint(errors, $"points[{index}]", value)).ToList();

        return points.OfType<LocationPoint>().ToList();
    }

    private static LocationPoint? ReadPoint(FieldErrorCollector errors, string field, TripLocationPointRequest? value)
    {
        if (value is not { Seq: >= 1, Latitude: >= -90 and <= 90, Longitude: >= -180 and <= 180, RecordedAt: not null })
        {
            errors.Add(field, "seq (≥ 1), latitude (-90 to 90), longitude (-180 to 180) and recordedAt are required");
            return null;
        }

        if (!HasValidOptionals(value))
        {
            errors.Add(field, "accuracyMeters and speedKmh must be ≥ 0; headingDegrees must be 0 to 360");
            return null;
        }

        return new LocationPoint(
            value.Seq.Value, value.Latitude.Value, value.Longitude.Value,
            value.AccuracyMeters, value.HeadingDegrees, value.SpeedKmh, value.RecordedAt.Value.ToUniversalTime());
    }

    private static bool HasValidOptionals(TripLocationPointRequest value) =>
        value.AccuracyMeters is null or >= 0
        && value.SpeedKmh is null or >= 0
        && value.HeadingDegrees is null or (>= 0 and <= 360);
}
