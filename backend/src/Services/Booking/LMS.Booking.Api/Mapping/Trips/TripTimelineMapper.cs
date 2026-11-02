using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Api.Contracts.Trips;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Api.Mapping.Trips;

// 行程时间线：创建（下单时间与经办人）、每次修改、司机指派与履约、取消，全部来自订单已记录的事实。
internal static class TripTimelineMapper
{
    private const string CreatedType = "CREATED";
    private const string ModifiedType = "MODIFIED";

    public static IReadOnlyList<TripTimelineEventView> ToTimeline(Trip trip) =>
        [.. new[] { Created(trip) }.Concat(Modifications(trip)).Concat(trip.Events.Select(ToEvent)).OrderBy(item => item.OccurredAt)];

    public static TripModificationView? ToView(TripModification? modification) => modification is null
        ? null
        : new TripModificationView(
            modification.Reason is null ? null : WireEnum.ToWire(modification.Reason.Value),
            modification.Comments,
            modification.ModifiedBy,
            modification.ModifiedByName,
            modification.ModifiedAt,
            modification.ChangedFields);

    private static TripTimelineEventView Created(Trip trip) => new(
        $"created:{trip.TripId}", CreatedType, trip.CreatedAt, null, null, trip.BookedBy.UserId, trip.BookedBy.Name, null, null);

    private static IEnumerable<TripTimelineEventView> Modifications(Trip trip) =>
        (trip.Modifications ?? []).Select((item, index) => new TripTimelineEventView(
            $"modified:{trip.TripId}:{index + 1}",
            ModifiedType,
            item.ModifiedAt,
            null,
            null,
            item.ModifiedBy,
            item.ModifiedByName,
            item.Reason is null ? null : WireEnum.ToWire(item.Reason.Value),
            item.ChangedFields));

    private static TripTimelineEventView ToEvent(TripEvent item) => new(
        item.EventId,
        WireEnum.ToWire(item.Type),
        item.OccurredAt,
        WireEnum.ToWire(item.Status),
        WireEnum.ToWire(TripStages.OfEvent(item.Type)),
        item.Actor,
        null,
        null,
        null);
}
