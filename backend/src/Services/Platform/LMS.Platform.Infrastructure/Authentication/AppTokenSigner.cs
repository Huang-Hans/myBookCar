using System.Security.Cryptography;
using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Identity.Options;
using LMS.Platform.Application.Authentication.Models;
using LMS.Platform.Application.Authentication.Ports;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LMS.Platform.Infrastructure.Authentication;

// 用 Platform 持有的 RSA 私钥以 RS256 签发 LmsAppBearer 令牌：司机/客户访问令牌，以及调用 Dispatch / Booking
// 内部凭据核对接口时使用的短效服务令牌。验证方只持有公钥。
internal sealed class AppTokenSigner : IAppAccessTokenIssuer, IDisposable
{
    private static readonly TimeSpan ServiceTokenLifetime = TimeSpan.FromMinutes(1);

    private readonly RSA? privateKey;
    private readonly SigningCredentials? credentials;
    private readonly AppIdentityOptions identity;
    private readonly JsonWebTokenHandler handler = new();

    public AppTokenSigner(IOptions<AppSigningOptions> signing, IOptions<AppIdentityOptions> identity)
    {
        this.identity = identity.Value;
        if (!signing.Value.IsConfigured)
        {
            return;
        }

        privateKey = RSA.Create();
        privateKey.ImportFromPem(signing.Value.PrivateKeyPem);
        credentials = new SigningCredentials(new RsaSecurityKey(privateKey) { KeyId = this.identity.KeyId }, SecurityAlgorithms.RsaSha256);
    }

    public bool IsAvailable => credentials is not null;

    public string Issue(AppAccessTokenRequest request)
    {
        var wire = WireEnum.ToWire(request.PrincipalType);
        var claims = new Dictionary<string, object>
        {
            [AppTokenClaims.Subject] = request.EntityId,
            [AppTokenClaims.PrincipalType] = wire,
            [AppTokenClaims.Roles] = new[] { wire },
            [AppTokenClaims.Scope] = string.Join(' ', PrincipalPermissionCodes.For(request.PrincipalType)),
            [AppTokenClaims.SessionId] = request.FamilyId
        };

        return Create(claims, request.IssuedAt, request.Lifetime);
    }

    // 未登录阶段没有业务令牌，Platform 以服务身份调用所属服务的内部凭据核对接口。
    public string IssueServiceToken()
    {
        var claims = new Dictionary<string, object>
        {
            [AppTokenClaims.Subject] = AppServiceIdentity.PlatformSubject,
            [AppTokenClaims.PrincipalType] = AppServiceIdentity.PrincipalType,
            [AppTokenClaims.Roles] = new[] { AppServiceIdentity.PrincipalType }
        };

        return Create(claims, DateTimeOffset.UtcNow, ServiceTokenLifetime);
    }

    public void Dispose() => privateKey?.Dispose();

    private string Create(Dictionary<string, object> claims, DateTimeOffset issuedAt, TimeSpan lifetime)
    {
        claims[AppTokenClaims.TokenId] = Guid.CreateVersion7().ToString("N");

        return handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = identity.Issuer,
            Audience = identity.Audience,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = (issuedAt + lifetime).UtcDateTime,
            Claims = claims,
            SigningCredentials = credentials ?? throw new InvalidOperationException("The app token signing key is not configured.")
        });
    }
}
