namespace LMS.Booking.Application.Directory;

public enum CallerLookupOutcome
{
    Found,

    // 令牌有效，但没有关联到可用的应用用户（或被 Platform 拒绝）。
    NotLinked,

    // Platform 暂时不可用，无法判断授权。
    SourceUnavailable
}
