using LMS.ApiGateway.Contracts.Authentication;
using LMS.BuildingBlocks.Http.Requests;

namespace LMS.ApiGateway.Requests.Authentication;

internal static class LoginRequestValidation
{
    private const string UsernameField = "username";
    private const string PasswordField = "password";

    public static IResult? Validate(LoginRequest request)
    {
        var errors = CollectErrors(request);

        return errors.HasErrors ? errors.ToFailure() : null;
    }

    private static FieldErrorCollector CollectErrors(LoginRequest request)
    {
        var errors = new FieldErrorCollector();
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            errors.Add(UsernameField, "Is required and must not be blank");
        }

        // Passwords must not be trimmed or normalized.
        if (string.IsNullOrEmpty(request.Password))
        {
            errors.Add(PasswordField, "Is required and must not be empty");
        }

        return errors;
    }
}
