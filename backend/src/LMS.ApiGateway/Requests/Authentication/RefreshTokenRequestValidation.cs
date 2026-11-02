using LMS.ApiGateway.Contracts.Authentication;
using LMS.BuildingBlocks.Http.Requests;

namespace LMS.ApiGateway.Requests.Authentication;

internal static class RefreshTokenRequestValidation
{
    private const string RefreshTokenField = "refreshToken";

    public static IResult? Validate(RefreshTokenRequest request)
    {
        var errors = new FieldErrorCollector();
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            errors.Add(RefreshTokenField, "Is required and must not be blank");
        }

        return errors.HasErrors ? errors.ToFailure() : null;
    }
}
