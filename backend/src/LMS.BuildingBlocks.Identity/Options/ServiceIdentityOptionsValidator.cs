using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace LMS.BuildingBlocks.Identity.Options;

internal sealed class ServiceIdentityOptionsValidator : IValidateOptions<ServiceIdentityOptions>
{
    private const int MinimumKeySizeBits = 2048;

    public ValidateOptionsResult Validate(string? name, ServiceIdentityOptions options)
    {
        if (!options.IsConfigured)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();
        Require(failures, !string.IsNullOrWhiteSpace(options.Issuer), nameof(ServiceIdentityOptions.Issuer), "must not be blank");
        Require(failures, !string.IsNullOrWhiteSpace(options.Audience), nameof(ServiceIdentityOptions.Audience), "must not be blank");
        Require(failures, !string.IsNullOrWhiteSpace(options.KeyId), nameof(ServiceIdentityOptions.KeyId), "must not be blank");
        Require(failures, HasUsablePublicKey(options.PublicKeyPem), nameof(ServiceIdentityOptions.PublicKeyPem),
            $"must be a PEM encoded RSA public key of at least {MinimumKeySizeBits} bits");

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static bool HasUsablePublicKey(string pem)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(pem);

            return rsa.KeySize >= MinimumKeySizeBits;
        }
        catch (Exception exception) when (exception is CryptographicException or ArgumentException)
        {
            return false;
        }
    }

    private static void Require(List<string> failures, bool isValid, string setting, string requirement)
    {
        if (!isValid)
        {
            failures.Add($"{ServiceIdentityOptions.SectionName}:{setting} {requirement}.");
        }
    }
}
