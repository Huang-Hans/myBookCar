using System.Security.Cryptography;
using LMS.BuildingBlocks.Core.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LMS.Booking.Infrastructure.ServiceIdentity;

// 用 Booking 持有的独立 RSA 私钥以 RS256 签发 ServiceBearer 服务令牌：subject 固定为 Booking 服务身份，
// principalType 为 SERVICE，scope 只含一个受限范围，有效期很短。它不是员工、司机或客户令牌，
// Platform 只在对应的内部接口接受它。
internal sealed class ServiceTokenSigner : IServiceTokenIssuer, IDisposable
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(1);

    private readonly RSA? privateKey;
    private readonly SigningCredentials? credentials;
    private readonly ServiceSigningOptions options;
    private readonly TimeProvider timeProvider;
    private readonly JsonWebTokenHandler handler = new();

    public ServiceTokenSigner(IOptions<ServiceSigningOptions> options, TimeProvider timeProvider)
    {
        this.options = options.Value;
        this.timeProvider = timeProvider;
        if (!this.options.IsConfigured)
        {
            return;
        }

        privateKey = RSA.Create();
        privateKey.ImportFromPem(this.options.PrivateKeyPem);
        credentials = new SigningCredentials(new RsaSecurityKey(privateKey) { KeyId = this.options.KeyId }, SecurityAlgorithms.RsaSha256);
    }

    public bool IsAvailable => credentials is not null;

    public string Issue(string scope)
    {
        var now = timeProvider.GetUtcNow();

        return handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = (now + Lifetime).UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [AppTokenClaims.Subject] = AppServiceIdentity.BookingSubject,
                [AppTokenClaims.PrincipalType] = AppServiceIdentity.PrincipalType,
                [AppTokenClaims.Roles] = new[] { AppServiceIdentity.PrincipalType },
                [AppTokenClaims.Scope] = scope,
                [AppTokenClaims.TokenId] = Guid.CreateVersion7().ToString("N")
            },
            SigningCredentials = credentials ?? throw new InvalidOperationException("The service signing key is not configured.")
        });
    }

    public void Dispose() => privateKey?.Dispose();
}
