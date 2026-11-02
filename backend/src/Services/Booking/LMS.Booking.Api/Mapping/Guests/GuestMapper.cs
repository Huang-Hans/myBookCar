using LMS.Booking.Api.Contracts.Guests;
using LMS.Booking.Api.Contracts.Trips;
using LMS.Booking.Api.Mapping.Trips;
using LMS.Booking.Application.Guests.Models;
using LMS.Booking.Domain.Guests;

namespace LMS.Booking.Api.Mapping.Guests;

internal static class GuestMapper
{
    public static GuestSummaryView ToSummaryView(GuestProfile guest) => new(
        guest.GuestId,
        guest.Name,
        guest.RoomNumber,
        guest.MemberId,
        guest.TierCode,
        guest.InHouse,
        guest.PropertyId);

    public static GuestProfileView ToProfileView(GuestProfileBundle bundle)
    {
        var restriction = bundle.BlacklistStatus.FirstOrDefault();

        return new GuestProfileView(
            bundle.Guest.GuestId,
            bundle.Guest.Name,
            bundle.Guest.TierCode,
            bundle.Guest.MemberId,
            bundle.Guest.RoomNumber,
            bundle.Guest.PropertyId,
            bundle.Guest.StayFrom,
            bundle.Guest.StayTo,
            bundle.Guest.PointsBalance,
            bundle.Guest.MaskedPhone,
            bundle.Guest.InHouse,
            new GuestBlacklistStatusView(restriction?.Listed == true, restriction?.Reason),
            new GuestPreferencesView(bundle.Guest.PreferredLanguage, bundle.Guest.PreferredVehicleClass, bundle.Guest.ConfirmationChannel),
            bundle.Guest.SpecialRequests,
            bundle.RecentTrips.Select(TripMapper.ToView).ToList());
    }

    public static GuestQualificationCheckView ToView(GuestQualificationCheck check) => new(
        check.Tier,
        check.TierMinimum,
        check.PlayVerified,
        check.PlayWindowDays,
        check.EntitledVehicleClasses.ToList(),
        check.PointsAvailable,
        check.Qualified);
}
