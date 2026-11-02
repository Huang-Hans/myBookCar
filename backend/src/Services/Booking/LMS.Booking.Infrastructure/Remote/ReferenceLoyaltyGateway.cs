using LMS.Booking.Application.Directory;
using LMS.Booking.Application.Guests.Ports;

namespace LMS.Booking.Infrastructure.Remote;

// ILoyaltyGateway 的一期实现：等级、游玩核验与积分直接取宾客读模型快照（reference，实施基线 D4 一期）。
// 二期替换为会员商城 API 客户端时只需更换本实现。
internal sealed class ReferenceLoyaltyGateway(IGuestDirectory guestDirectory) : ILoyaltyGateway
{
    public async Task<DirectoryResult<LoyaltyQualification>> GetQualificationAsync(string guestId, CancellationToken cancellationToken)
    {
        var guest = await guestDirectory.FindAsync(guestId, cancellationToken);

        return guest is null
            ? DirectoryResult<LoyaltyQualification>.Failed(DirectoryOutcome.NotFound)
            : DirectoryResult<LoyaltyQualification>.Success(
                new LoyaltyQualification(guest.TierCode, guest.PlayVerified, guest.PlayWindowDays, guest.PointsBalance));
    }
}
