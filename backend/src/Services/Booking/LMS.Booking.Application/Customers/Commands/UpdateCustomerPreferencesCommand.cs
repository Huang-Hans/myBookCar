using LMS.BuildingBlocks.Core.Patching;
using LMS.BuildingBlocks.Core.Preferences;
using LMS.Booking.Application.Abstractions;

namespace LMS.Booking.Application.Customers.Commands;

public sealed record UpdateCustomerPreferencesCommand(
    string GuestId,
    Optional<AppLanguage> Language,
    Optional<bool> IsPushNotificationsEnabled,
    long ExpectedVersion,
    CommandContext Context);
