using System.Security.Cryptography;
using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Identity.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace LMS.BuildingBlocks.Identity.Authentication;

internal static class AppTokenValidation
{
    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    // 只接受本地发行方、本地受众、RS256 与受控公钥签名的令牌；不使用 Authority / 元数据地址。
    public static void Configure(JwtBearerOptions bearer, AppIdentityOptions options)
    {
        bearer.MapInboundClaims = false;
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateLifetime = true,
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = CreateSigningKey(options),
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            NameClaimType = AppTokenClaims.Subject,
            RoleClaimType = AppTokenClaims.Roles,
            ClockSkew = ClockSkew
        };
        bearer.Events = new JwtBearerEvents
        {
            OnMessageReceived = HubAccessTokenReader.OnMessageReceivedAsync,
            OnChallenge = JwtBearerChallengeHandler.OnChallengeAsync
        };
    }

    // RsaSecurityKey 持有的 RSA 实例随应用生命周期存在，不在此释放。
    private static RsaSecurityKey CreateSigningKey(AppIdentityOptions options)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(options.PublicKeyPem);

        return new RsaSecurityKey(rsa) { KeyId = options.KeyId };
    }
}
