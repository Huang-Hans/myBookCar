namespace LMS.Platform.Infrastructure.Authentication;

internal sealed class BookingCredentialClient(HttpClient client, AppTokenSigner signer) : CredentialServiceClient(client, signer)
{
    protected override string VerificationPath => "internal/v1/customer-credential-verifications";

    protected override string EligibilityPath => "internal/v1/customer-login-eligibilities";
}
