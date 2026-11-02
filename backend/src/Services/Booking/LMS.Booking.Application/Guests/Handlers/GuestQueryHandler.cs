using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Directory;
using LMS.Booking.Application.Console.Options;
using LMS.Booking.Application.Guests.Models;
using LMS.Booking.Application.Guests.Ports;
using LMS.Booking.Application.Trips.Ports;
using LMS.Booking.Application.Trips.Services;
using LMS.Booking.Domain.Guests;
using LMS.Booking.Domain.Trips;
using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using Microsoft.Extensions.Options;

namespace LMS.Booking.Application.Guests.Handlers;

// 宾客检索与档案（HA-01/HA-06）与资格判定（HA-02）。
// 档案的黑名单状态实时读 Platform；判定失败时旁路登记待经理复核档案（D1）。
internal sealed class GuestQueryHandler(
    IGuestDirectory guestDirectory,
    IPlatformDirectory platform,
    ILoyaltyGateway loyalty,
    ITripRepository trips,
    IOptions<ConsoleOptions> consoleOptions,
    QualificationRecorder qualificationRecorder) : IGuestQueryHandler
{
    public async Task<OperationResult<PageResult<GuestProfile>>> SearchAsync(ListGuestsQuery query, CancellationToken cancellationToken) =>
        OperationResult<PageResult<GuestProfile>>.Succeeded(
            await guestDirectory.SearchAsync(query.List, query.TierCode, query.Scope, cancellationToken));

    public async Task<OperationResult<GuestProfileBundle>> GetProfileAsync(string guestId, PropertyScope scope, CancellationToken cancellationToken)
    {
        var guest = await guestDirectory.FindAsync(guestId, cancellationToken);
        if (guest is null || !scope.Allows(guest.PropertyId ?? string.Empty))
        {
            return OperationResult<GuestProfileBundle>.Failed(OperationOutcome.NotFound);
        }

        if (!string.IsNullOrEmpty(guest.MemberId))
        {
            var restrictions = await platform.CheckCustomersAsync([guest.MemberId], cancellationToken);
            if (!restrictions.IsSuccess)
            {
                return OperationResult<GuestProfileBundle>.Failed(
                    restrictions.Outcome == DirectoryOutcome.Forbidden
                        ? OperationOutcome.Forbidden
                        : OperationOutcome.SourceUnavailable);
            }

            if (restrictions.Value!.Count > 0)
            {
                return OperationResult<GuestProfileBundle>.Succeeded(new GuestProfileBundle(
                    guest,
                    [new GuestRestriction(true, restrictions.Value[0].Reason)],
                    await trips.ListRecentByGuestAsync(guestId, scope, 5, cancellationToken)));
            }
        }

        return OperationResult<GuestProfileBundle>.Succeeded(new GuestProfileBundle(
            guest,
            [new GuestRestriction(false, null)],
            await trips.ListRecentByGuestAsync(guestId, scope, 5, cancellationToken)));
    }

    public async Task<OperationResult<GuestQualificationCheck>> CheckQualificationAsync(
        string guestId,
        string propertyId,
        PropertyScope scope,
        CommandContext context,
        CancellationToken cancellationToken)
    {
        if (!scope.Allows(propertyId))
        {
            return OperationResult<GuestQualificationCheck>.Failed(OperationOutcome.Forbidden);
        }

        var guest = await guestDirectory.FindAsync(guestId, cancellationToken);
        if (guest is null)
        {
            return OperationResult<GuestQualificationCheck>.Failed(OperationOutcome.NotFound);
        }

        var qualification = await loyalty.GetQualificationAsync(guestId, cancellationToken);
        if (!qualification.IsSuccess)
        {
            return qualification.Outcome switch
            {
                DirectoryOutcome.NotFound => OperationResult<GuestQualificationCheck>.Failed(OperationOutcome.NotFound),
                DirectoryOutcome.Forbidden => OperationResult<GuestQualificationCheck>.Failed(OperationOutcome.Forbidden),
                _ => OperationResult<GuestQualificationCheck>.Failed(OperationOutcome.SourceUnavailable)
            };
        }

        var loyaltyInfo = qualification.Value!;
        var entitlement = EntitlementOf(loyaltyInfo.TierCode);
        var check = new GuestQualificationCheck(
            loyaltyInfo.TierCode,
            TierMinimum(),
            loyaltyInfo.PlayVerified,
            loyaltyInfo.PlayWindowDays,
            entitlement?.VehicleClasses.ToList() ?? [],
            loyaltyInfo.PointsBalance,
            entitlement is not null && entitlement.VehicleClasses.Count > 0 && loyaltyInfo.PlayVerified);

        if (!check.Qualified)
        {
            await qualificationRecorder.RecordRequiredAsync(
                guest, propertyId, "Qualification check did not pass", context, null, cancellationToken);
        }

        return OperationResult<GuestQualificationCheck>.Succeeded(check);
    }

    private TierEntitlementOption? EntitlementOf(string tierCode) =>
        consoleOptions.Value.EffectiveTierEntitlements.FirstOrDefault(tier =>
            string.Equals(tier.TierCode, tierCode, StringComparison.OrdinalIgnoreCase));

    // 最低可参与等级：配置的等级权益中，自低到高第一个含可用车型的等级。
    private string? TierMinimum() =>
        TierCodes.Ascending
            .Select(EntitlementOf)
            .FirstOrDefault(entitlement => entitlement is { VehicleClasses.Count: > 0 })
            ?.TierCode;
}
