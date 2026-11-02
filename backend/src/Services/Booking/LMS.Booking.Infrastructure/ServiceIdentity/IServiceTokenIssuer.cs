namespace LMS.Booking.Infrastructure.ServiceIdentity;

internal interface IServiceTokenIssuer
{
    bool IsAvailable { get; }

    // 签发只含单一受限范围的短效服务令牌；未配置时不可调用。
    string Issue(string scope);
}
