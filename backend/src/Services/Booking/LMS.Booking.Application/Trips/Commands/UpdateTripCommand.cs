using LMS.BuildingBlocks.Core.Patching;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.Trips.Commands;

// 修改行程：字段为三态 Optional；地点名称与路线由处理器在事务外解析后整体替换。
// ModificationReason / ModificationComments 是本次修改的原因记录（客服选择），不是行程字段；不提供时记录为空。
public sealed record UpdateTripCommand(
    string TripId,
    long ExpectedVersion,
    Optional<string> PickupLocationId,
    Optional<string> DropoffLocationId,
    Optional<DateTimeOffset> PickupAt,
    Optional<PickupMode> PickupMode,
    Optional<ServiceType> ServiceType,
    Optional<VehicleClass> VehicleClass,
    Optional<int> Passengers,
    Optional<int> Luggage,
    Optional<string?> SpecialRequests,
    Optional<string> ConfirmationLanguage,
    CommandContext Context,
    TripModificationReason? ModificationReason = null,
    string? ModificationComments = null)
{
    public IReadOnlyList<string> ChangedFields() =>
        ChangedFieldNames.From(
            ("route.pickupLocationId", PickupLocationId.IsSet),
            ("route.dropoffLocationId", DropoffLocationId.IsSet),
            ("pickupAt", PickupAt.IsSet),
            ("pickupMode", PickupMode.IsSet),
            ("serviceType", ServiceType.IsSet),
            ("vehicleClass", VehicleClass.IsSet),
            ("passengers", Passengers.IsSet),
            ("luggage", Luggage.IsSet),
            ("specialRequests", SpecialRequests.IsSet),
            ("confirmationLanguage", ConfirmationLanguage.IsSet));
}
