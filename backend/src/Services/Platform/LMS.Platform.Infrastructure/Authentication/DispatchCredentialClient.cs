namespace LMS.Platform.Infrastructure.Authentication;

internal sealed class DispatchCredentialClient(HttpClient client, AppTokenSigner signer) : CredentialServiceClient(client, signer)
{
    protected override string VerificationPath => "internal/v1/driver-credential-verifications";

    protected override string EligibilityPath => "internal/v1/driver-login-eligibilities";
}
