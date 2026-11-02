using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Coupons.Commands;
using LMS.Dispatch.Application.Coupons.Models;
using LMS.Dispatch.Application.Coupons.Ports;
using LMS.Dispatch.Application.Drivers.Services;
using LMS.Dispatch.Domain.Coupons;

namespace LMS.Dispatch.Application.Coupons.Handlers;

// 優惠券訂單核銷與退還。同一 redemptionKey 只計一次；取消後退還，重試同一鍵不會重複扣減。
internal sealed class CouponRedemptionHandler(
    ICouponRepository coupons,
    ICouponRedemptionRepository redemptions,
    ITransactionRunner transactions,
    BusinessCalendar calendar) : ICouponRedemptionHandler
{
    private const int Attempts = 3;

    public async Task<OperationResult<CouponRedemptionResult>> RedeemAsync(RedeemCouponCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RedemptionKey) || string.IsNullOrWhiteSpace(command.CouponId))
        {
            return OperationResult<CouponRedemptionResult>.Invalid([new FieldIssue("couponId", "Coupon was not found.")]);
        }

        var key = command.RedemptionKey.Trim();
        var couponId = command.CouponId.Trim();
        for (var attempt = 0; attempt < Attempts; attempt++)
        {
            var existing = await redemptions.FindAsync(key, cancellationToken);
            if (existing is { Restored: false })
            {
                return await AlreadyRedeemedAsync(existing, couponId, cancellationToken);
            }

            var coupon = await coupons.FindByIdAsync(couponId, cancellationToken);
            if (coupon is null)
            {
                return OperationResult<CouponRedemptionResult>.Rejected([new FieldIssue("couponId", "Coupon was not found.")]);
            }

            var issues = Unusable(coupon, command, calendar.Today());
            if (issues.Count > 0)
            {
                return OperationResult<CouponRedemptionResult>.Rejected(issues);
            }

            try
            {
                var written = await transactions.ExecuteAsync(
                    (context, token) => WriteRedemptionAsync(key, couponId, command, context, token),
                    cancellationToken);
                if (written is not null)
                {
                    return written;
                }
            }
            catch (Exception exception) when (exception is DuplicateRecordException or CouponWriteConflictException)
            {
                // 併發的同一鍵或版本衝突：整筆事務已回滾，讀到勝者後返回。
            }
        }

        return OperationResult<CouponRedemptionResult>.Failed(OperationOutcome.StateConflict);
    }

    public async Task<OperationResult<CouponRedemptionResult>> RestoreAsync(string redemptionKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(redemptionKey))
        {
            return OperationResult<CouponRedemptionResult>.Succeeded(new CouponRedemptionResult(string.Empty, string.Empty, 0, true));
        }

        var key = redemptionKey.Trim();
        for (var attempt = 0; attempt < Attempts; attempt++)
        {
            var existing = await redemptions.FindAsync(key, cancellationToken);
            if (existing is null || existing.Restored)
            {
                var coupon = existing is null ? null : await coupons.FindByIdAsync(existing.CouponId, cancellationToken);
                return OperationResult<CouponRedemptionResult>.Succeeded(
                    new CouponRedemptionResult(key, existing?.CouponId ?? string.Empty, coupon?.RedeemedCount ?? 0, true));
            }

            try
            {
                var written = await transactions.ExecuteAsync(
                    (context, token) => WriteRestoreAsync(key, context, token),
                    cancellationToken);
                if (written is not null)
                {
                    return written;
                }
            }
            catch (CouponWriteConflictException)
            {
                // 版本衝突，重讀後再退還一次。
            }
        }

        return OperationResult<CouponRedemptionResult>.Failed(OperationOutcome.StateConflict);
    }

    private async Task<OperationResult<CouponRedemptionResult>> AlreadyRedeemedAsync(
        CouponRedemption existing,
        string couponId,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(existing.CouponId, couponId, StringComparison.Ordinal))
        {
            return OperationResult<CouponRedemptionResult>.Rejected([new FieldIssue("couponId", "This coupon cannot be used.")]);
        }

        var coupon = await coupons.FindByIdAsync(existing.CouponId, cancellationToken);
        return coupon is null
            ? OperationResult<CouponRedemptionResult>.Rejected([new FieldIssue("couponId", "Coupon was not found.")])
            : OperationResult<CouponRedemptionResult>.Succeeded(new CouponRedemptionResult(existing.RedemptionKey, coupon.CouponId, coupon.RedeemedCount, false));
    }

    private async Task<OperationResult<CouponRedemptionResult>?> WriteRedemptionAsync(
        string key,
        string couponId,
        RedeemCouponCommand command,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var raced = await redemptions.FindAsync(key, context, cancellationToken);
        if (raced is { Restored: false })
        {
            var current = await coupons.FindByIdAsync(raced.CouponId, context, cancellationToken);
            return current is null
                ? OperationResult<CouponRedemptionResult>.Rejected([new FieldIssue("couponId", "Coupon was not found.")])
                : OperationResult<CouponRedemptionResult>.Succeeded(new CouponRedemptionResult(key, current.CouponId, current.RedeemedCount, false));
        }

        var loaded = await coupons.FindByIdAsync(couponId, context, cancellationToken);
        if (loaded is null)
        {
            return OperationResult<CouponRedemptionResult>.Rejected([new FieldIssue("couponId", "Coupon was not found.")]);
        }

        var issues = Unusable(loaded, command, calendar.Today());
        if (issues.Count > 0)
        {
            return OperationResult<CouponRedemptionResult>.Rejected(issues);
        }

        if (raced is null)
        {
            await redemptions.InsertAsync(new CouponRedemption(key, couponId, false), context, cancellationToken);
        }
        else if (!await redemptions.TrySetRestoredAsync(key, false, context, cancellationToken))
        {
            throw new CouponWriteConflictException();
        }

        var redeemed = loaded.RedeemOne();
        if (!await coupons.TryReplaceAsync(redeemed, loaded.Version, context, cancellationToken))
        {
            throw new CouponWriteConflictException();
        }

        return OperationResult<CouponRedemptionResult>.Succeeded(new CouponRedemptionResult(key, redeemed.CouponId, redeemed.RedeemedCount, false));
    }

    private async Task<OperationResult<CouponRedemptionResult>?> WriteRestoreAsync(
        string key,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var raced = await redemptions.FindAsync(key, context, cancellationToken);
        if (raced is null || raced.Restored)
        {
            var unchanged = raced is null ? null : await coupons.FindByIdAsync(raced.CouponId, context, cancellationToken);
            return OperationResult<CouponRedemptionResult>.Succeeded(
                new CouponRedemptionResult(key, raced?.CouponId ?? string.Empty, unchanged?.RedeemedCount ?? 0, true));
        }

        var loaded = await coupons.FindByIdAsync(raced.CouponId, context, cancellationToken);
        if (loaded is { RedeemedCount: > 0 })
        {
            var restored = loaded.RestoreOne();
            if (!await coupons.TryReplaceAsync(restored, loaded.Version, context, cancellationToken))
            {
                throw new CouponWriteConflictException();
            }

            loaded = restored;
        }

        if (!await redemptions.TrySetRestoredAsync(key, true, context, cancellationToken))
        {
            throw new CouponWriteConflictException();
        }

        return OperationResult<CouponRedemptionResult>.Succeeded(
            new CouponRedemptionResult(key, raced.CouponId, loaded?.RedeemedCount ?? 0, true));
    }

    private static List<FieldIssue> Unusable(Coupon coupon, RedeemCouponCommand command, DateOnly today)
    {
        var issues = new List<FieldIssue>();
        var boundTo = coupon.MemberId.Trim();
        var matchesMember = !string.IsNullOrWhiteSpace(command.MemberId)
            && string.Equals(boundTo, command.MemberId.Trim(), StringComparison.OrdinalIgnoreCase);
        var matchesGuest = !string.IsNullOrWhiteSpace(command.GuestId)
            && string.Equals(boundTo, command.GuestId.Trim(), StringComparison.OrdinalIgnoreCase);
        if (!matchesMember && !matchesGuest)
        {
            issues.Add(new FieldIssue("couponId", "This coupon belongs to another member."));
        }

        if (coupon.Scope == CouponScope.Property
            && !string.Equals(coupon.PropertyId, command.PropertyId?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new FieldIssue("couponId", "This coupon does not cover the selected property."));
        }

        if (coupon.VehicleRestriction is not null
            && !string.Equals(WireEnum.ToWire(coupon.VehicleRestriction.Value), command.VehicleClass, StringComparison.Ordinal))
        {
            issues.Add(new FieldIssue("couponId", "This coupon does not cover the selected vehicle."));
        }

        if (coupon.Disabled || today < coupon.ValidFrom || today > coupon.ValidTo)
        {
            issues.Add(new FieldIssue("couponId", "This coupon is not active."));
        }
        else if (coupon.RedeemedCount >= coupon.IssuedQuantity)
        {
            issues.Add(new FieldIssue("couponId", "This coupon has no remaining uses."));
        }

        return issues;
    }

    private sealed class CouponWriteConflictException : Exception;
}
