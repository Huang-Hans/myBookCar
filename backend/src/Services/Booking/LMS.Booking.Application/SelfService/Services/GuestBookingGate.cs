using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Directory;
using LMS.Booking.Application.Guests.Ports;
using LMS.Booking.Application.SelfService.Models;
using LMS.Booking.Application.SelfService.Ports;

namespace LMS.Booking.Application.SelfService.Services;

// 客户自助预订的宾客级前置检查（与具体行程无关，供首页目录、车型查询与建单共用，因此三处的结论一致）：
// 宾客存在 → 读取 Platform 自助规则与资格说明 → 物业归属 → 等级资格 → 客户黑名单。
// 不满足预订条件不是错误，而是 BookingBlock，由调用方决定是返回不可订状态还是拒绝建单；
// 宾客不存在为 NotFound，Platform 不可用为 SourceUnavailable，不用默认值代替。
internal sealed class GuestBookingGate(IGuestDirectory guests, IPlatformSelfCatalog catalog)
{
    public async Task<OperationResult<GuestBookingContext>> LoadAsync(string guestId, CancellationToken cancellationToken)
    {
        var guest = await guests.FindAsync(guestId, cancellationToken);
        if (guest is null)
        {
            return OperationResult<GuestBookingContext>.Failed(OperationOutcome.NotFound);
        }

        var rules = await catalog.GetBookingRulesAsync(cancellationToken);
        var eligibility = await catalog.GetEligibilityRulesAsync(cancellationToken);
        if (!rules.IsSuccess || !eligibility.IsSuccess)
        {
            return OperationResult<GuestBookingContext>.Failed(OperationOutcome.SourceUnavailable);
        }

        var block = await FindBlockAsync(guest, eligibility.Value!, cancellationToken);

        return block.Outcome == OperationOutcome.SourceUnavailable
            ? OperationResult<GuestBookingContext>.Failed(OperationOutcome.SourceUnavailable)
            : OperationResult<GuestBookingContext>.Succeeded(new GuestBookingContext(guest, rules.Value!, block.Value));
    }

    private async Task<OperationResult<BookingBlock?>> FindBlockAsync(
        Domain.Guests.GuestProfile guest,
        SelfEligibilityRules eligibility,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(guest.PropertyId))
        {
            return Block(BookingBlock.PropertyUnassigned);
        }

        if (!eligibility.EligibleTierCodes.Contains(guest.TierCode, StringComparer.OrdinalIgnoreCase))
        {
            return Block(BookingBlock.TierNotEligible);
        }

        return await IsRestrictedAsync(guest, cancellationToken);
    }

    private async Task<OperationResult<BookingBlock?>> IsRestrictedAsync(Domain.Guests.GuestProfile guest, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(guest.MemberId))
        {
            return OperationResult<BookingBlock?>.Succeeded(null);
        }

        var restrictions = await catalog.CheckRestrictionsAsync([guest.MemberId], cancellationToken);
        if (!restrictions.IsSuccess)
        {
            return OperationResult<BookingBlock?>.Failed(OperationOutcome.SourceUnavailable);
        }

        return restrictions.Value!.Count > 0 ? Block(BookingBlock.Restricted) : OperationResult<BookingBlock?>.Succeeded(null);
    }

    private static OperationResult<BookingBlock?> Block(BookingBlock block) => OperationResult<BookingBlock?>.Succeeded(block);
}
