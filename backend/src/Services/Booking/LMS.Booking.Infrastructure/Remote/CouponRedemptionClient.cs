using System.Net.Http.Json;
using LMS.BuildingBlocks.Core.Authorization;
using LMS.Booking.Application.Coupons;
using LMS.Booking.Application.Directory;
using LMS.Booking.Infrastructure.ServiceIdentity;

namespace LMS.Booking.Infrastructure.Remote;

internal sealed class CouponRedemptionClient(HttpClient client, IServiceTokenIssuer tokens) : ICouponRedemptions
{
    public async Task<DirectoryResult<bool>> RedeemAsync(CouponUse use, CancellationToken cancellationToken)
    {
        if (!tokens.IsAvailable)
        {
            return DirectoryResult<bool>.Failed(DirectoryOutcome.Unavailable);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "internal/v1/coupon-redemptions")
        {
            Content = JsonContent.Create(new RedeemPayload(use.RedemptionKey, use.CouponId, use.MemberId, use.GuestId, use.PropertyId, use.VehicleClass))
        };
        var response = await RemoteHttp.SendAsync(client, request, tokens.Issue(ServiceScopes.CouponRedemptionWrite), cancellationToken);
        return await RemoteHttp.ReadAsync<RemoteCouponRedemption, bool>(response, _ => true, cancellationToken);
    }

    public async Task<DirectoryResult<bool>> RestoreAsync(string redemptionKey, CancellationToken cancellationToken)
    {
        if (!tokens.IsAvailable)
        {
            return DirectoryResult<bool>.Failed(DirectoryOutcome.Unavailable);
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"internal/v1/coupon-redemptions/{Uri.EscapeDataString(redemptionKey)}/restoration");
        var response = await RemoteHttp.SendAsync(client, request, tokens.Issue(ServiceScopes.CouponRedemptionWrite), cancellationToken);
        return await RemoteHttp.ReadAsync<RemoteCouponRedemption, bool>(response, _ => true, cancellationToken);
    }

    private sealed record RedeemPayload(string RedemptionKey, string CouponId, string? MemberId, string? GuestId, string PropertyId, string VehicleClass);

    private sealed record RemoteCouponRedemption(string RedemptionKey, string CouponId, int RedeemedCount, bool Restored);
}
