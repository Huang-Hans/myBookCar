using System.Security.Cryptography;
using LMS.BuildingBlocks.Identity.Options;
using Microsoft.Extensions.Options;

namespace LMS.Platform.Infrastructure.Authentication;

// 私钥必须可解析、至少 2048 位，并且与验证方使用的 AppIdentity 公钥成对；不成对会让所有已签发令牌被验证方拒绝。
internal sealed class AppSigningOptionsValidator(IOptions<AppIdentityOptions> identity) : IValidateOptions<AppSigningOptions>
{
    private const int MinimumKeySizeBits = 2048;

    public ValidateOptionsResult Validate(string? name, AppSigningOptions options)
    {
        if (!options.IsConfigured)
        {
            return ValidateOptionsResult.Success;
        }

        try
        {
            using var privateKey = RSA.Create();
            privateKey.ImportFromPem(options.PrivateKeyPem);

            return Check(privateKey);
        }
        catch (Exception exception) when (exception is CryptographicException or ArgumentException)
        {
            return ValidateOptionsResult.Fail($"{AppSigningOptions.SectionName}:PrivateKeyPem must be a PEM encoded RSA private key.");
        }
    }

    private ValidateOptionsResult Check(RSA privateKey)
    {
        if (privateKey.KeySize < MinimumKeySizeBits)
        {
            return ValidateOptionsResult.Fail($"{AppSigningOptions.SectionName}:PrivateKeyPem must be at least {MinimumKeySizeBits} bits.");
        }

        return PairsWithPublicKey(privateKey)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                $"{AppSigningOptions.SectionName}:PrivateKeyPem must pair with {AppIdentityOptions.SectionName}:PublicKeyPem.");
    }

    private bool PairsWithPublicKey(RSA privateKey)
    {
        if (!identity.Value.IsConfigured)
        {
            return false;
        }

        using var publicKey = RSA.Create();
        publicKey.ImportFromPem(identity.Value.PublicKeyPem);

        return privateKey.ExportRSAPublicKey().AsSpan().SequenceEqual(publicKey.ExportRSAPublicKey());
    }
}
