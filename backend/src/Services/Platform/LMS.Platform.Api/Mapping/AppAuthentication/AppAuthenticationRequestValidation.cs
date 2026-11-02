using LMS.BuildingBlocks.Core.Security;
using LMS.BuildingBlocks.Http.Requests;
using LMS.Platform.Api.Contracts.AppAuthentication;

namespace LMS.Platform.Api.Mapping.AppAuthentication;

internal static class AppAuthenticationRequestValidation
{
    private const string PhoneField = "phone";
    private const string PasswordField = "password";
    private const string RefreshTokenField = "refreshToken";

    public static IResult? Validate(AppLoginRequest request)
    {
        var errors = new FieldErrorCollector();
        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            errors.Add(PhoneField, "Is required and must not be blank");
        }
        else if (!PhoneNumber.TryNormalize(request.Phone, out _))
        {
            errors.Add(PhoneField, "Must be a phone number, for example +85361234567 or 13800138001");
        }

        // 密码不去除空白、不规范化。
        if (string.IsNullOrEmpty(request.Password))
        {
            errors.Add(PasswordField, "Is required and must not be empty");
        }

        return errors.HasErrors ? errors.ToFailure() : null;
    }

    public static IResult? ValidateRefreshToken(string refreshToken)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var errors = new FieldErrorCollector();
        errors.Add(RefreshTokenField, "Is required and must not be blank");

        return errors.ToFailure();
    }
}
