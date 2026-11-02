using LMS.BuildingBlocks.Core.Patching;
using LMS.BuildingBlocks.Core.Preferences;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Requests;
using LMS.Booking.Api.Contracts.CustomerRecords;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Customers.Commands;
using LMS.Booking.Application.Customers.Models;
using LMS.Booking.Application.Reviews.Commands;
using LMS.Booking.Application.Reviews.Models;
using LMS.Booking.Domain.Customers;
using LMS.Booking.Domain.Reviews;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Api.Mapping.CustomerRecords;

internal static class CustomerRecordMapper
{
    private const string MembershipSource = "MEMBER_SNAPSHOT";

    public static CustomerAccountView ToView(CustomerAccount account) => new(
        account.Guest.GuestId,
        account.DisplayName,
        account.IsNameOverridden ? "LOCAL" : MembershipSource,
        account.Local.Email,
        account.Guest.MaskedPhone,
        false,
        account.Guest.MemberId,
        account.Guest.TierCode,
        account.Guest.PointsBalance,
        MembershipSource,
        account.Local.UpdatedAt,
        account.Local.Version);

    public static CustomerPreferencesView ToView(CustomerPreferences preferences) => new(
        WireEnum.ToWire(preferences.Language),
        preferences.IsPushNotificationsEnabled,
        new PreferenceDeliveryView(false),
        preferences.UpdatedAt,
        preferences.Version);

    public static LocationFavoritesView ToView(FavoritesResult result) => new(
        WireEnum.ToWire(result.Favorites.Usage),
        result.Items.Select(item => new FavoriteLocationView(item.Favorite.LocationId, item.Favorite.Name, item.Favorite.Region, item.IsAvailable)).ToList(),
        result.Favorites.UpdatedAt,
        result.Favorites.Version);

    public static TripReviewView ToView(TripReview review) => new(
        review.TripId, review.TripReference, review.Rating, review.Comments, review.CreatedAt, review.UpdatedAt, review.Version);

    public static ReviewableTripView ToView(ReviewableTrip item) => new(
        item.Trip.TripId,
        item.Trip.TripReference,
        item.Trip.Product is null ? null : WireEnum.ToWire(item.Trip.Product.Value),
        item.Trip.Route.PickupName,
        item.Trip.Route.DropoffName,
        item.Trip.PickupAt,
        item.Trip.ClosedAt,
        true,
        item.Review is null ? null : ToView(item.Review));

    public static RequestParseResult<UpdateCustomerAccountCommand> ToCommand(
        string guestId,
        long expectedVersion,
        UpdateCustomerAccountRequest request,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var name = errors.PatchText("name", request.Name);
        if (name.IsSet && name.Value!.Length > CustomerAccountProfile.MaxNameLength)
        {
            errors.Add("name", $"Must not exceed {CustomerAccountProfile.MaxNameLength} characters");
        }

        var email = request.Email.PatchNullableText();
        CheckEmail(errors, email);
        if (!name.IsSet && !email.IsSet)
        {
            errors.Add(string.Empty, "At least one field is required");
        }

        return errors.HasErrors
            ? RequestParseResult<UpdateCustomerAccountCommand>.Fail(errors.ToFailure())
            : RequestParseResult<UpdateCustomerAccountCommand>.Success(new UpdateCustomerAccountCommand(guestId, name, email, expectedVersion, context));
    }

    public static RequestParseResult<UpdateCustomerPreferencesCommand> ToCommand(
        string guestId,
        long expectedVersion,
        UpdateCustomerPreferencesRequest request,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var language = errors.PatchEnum<AppLanguage>("language", request.Language);
        var isPush = errors.PatchBool("isPushNotificationsEnabled", request.IsPushNotificationsEnabled);
        if (!language.IsSet && !isPush.IsSet)
        {
            errors.Add(string.Empty, "At least one field is required");
        }

        return errors.HasErrors
            ? RequestParseResult<UpdateCustomerPreferencesCommand>.Fail(errors.ToFailure())
            : RequestParseResult<UpdateCustomerPreferencesCommand>.Success(
                new UpdateCustomerPreferencesCommand(guestId, language, isPush, expectedVersion, context));
    }

    public static RequestParseResult<ReplaceLocationFavoritesCommand> ToCommand(
        string guestId,
        FavoriteUsage usage,
        long expectedVersion,
        ReplaceLocationFavoritesRequest request,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var ids = new List<string>();
        if (request.Items is null)
        {
            errors.Add("items", "Is required; use an empty array to clear the group");
        }
        else
        {
            for (var index = 0; index < request.Items.Count; index++)
            {
                ids.Add(errors.RequiredText($"items[{index}].locationId", request.Items[index]?.LocationId));
            }
        }

        return errors.HasErrors
            ? RequestParseResult<ReplaceLocationFavoritesCommand>.Fail(errors.ToFailure())
            : RequestParseResult<ReplaceLocationFavoritesCommand>.Success(
                new ReplaceLocationFavoritesCommand(guestId, usage, ids, expectedVersion, context));
    }

    public static RequestParseResult<UpsertTripReviewCommand> ToCommand(
        string tripId,
        string guestId,
        long expectedVersion,
        UpsertTripReviewRequest request,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var rating = errors.RequiredInt("rating", request.Rating);
        var comments = ReviewRules.NormalizeComments(request.Comments);
        errors.AddRange(ReviewRules.Validate(rating, comments));

        return errors.HasErrors
            ? RequestParseResult<UpsertTripReviewCommand>.Fail(errors.ToFailure())
            : RequestParseResult<UpsertTripReviewCommand>.Success(new UpsertTripReviewCommand(tripId, guestId, rating, comments, expectedVersion, context));
    }

    private static void CheckEmail(FieldErrorCollector errors, Optional<string> email)
    {
        if (!email.IsSet || email.Value is null)
        {
            return;
        }

        errors.RequiredEmail("email", email.Value);
        if (email.Value.Length > CustomerAccountProfile.MaxEmailLength)
        {
            errors.Add("email", $"Must not exceed {CustomerAccountProfile.MaxEmailLength} characters");
        }
    }
}
