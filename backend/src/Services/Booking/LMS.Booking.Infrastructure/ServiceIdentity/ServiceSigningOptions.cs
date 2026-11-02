namespace LMS.Booking.Infrastructure.ServiceIdentity;

// Booking 后台调用 Platform 受限内部接口（通知收件人查询）的服务身份签名配置。
// Each service's ServiceSigning and ServiceIdentity settings use the same RSA key pair, issuer, audience and kid.
// 私钥为空表示尚未配置：不签发任何服务令牌，收件人查询按“目录不可用”处理，待处理的客服下单通知保留重试，不会被当作零收件人完成。
// Read the PEM text from ServiceSigning:PrivateKeyPem in appsettings.json; never commit real keys.
public sealed class ServiceSigningOptions
{
    public const string SectionName = "ServiceSigning";

    public string Issuer { get; init; } = "lms-service";

    public string Audience { get; init; } = "lms-platform-internal";

    public string KeyId { get; init; } = "lms-service-1";

    public string PrivateKeyPem { get; init; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(PrivateKeyPem);
}
