using LMS.Booking.Application.Directory;

namespace LMS.Booking.Application.Guests.Ports;

// 会员/忠诚度端口（一期 reference：读宾客快照的等级、游玩核验与积分；二期接会员商城 API）。
public interface ILoyaltyGateway
{
    Task<DirectoryResult<LoyaltyQualification>> GetQualificationAsync(string guestId, CancellationToken cancellationToken);
}

public sealed record LoyaltyQualification(string TierCode, bool PlayVerified, int PlayWindowDays, long? PointsBalance);
