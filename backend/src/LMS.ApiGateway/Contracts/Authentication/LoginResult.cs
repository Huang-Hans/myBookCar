using LMS.BuildingBlocks.Http.Contracts;

namespace LMS.ApiGateway.Contracts.Authentication;

public sealed class LoginResult
{
    private LoginResult(LoginResponse? response, ApiError? error)
    {
        Response = response;
        Error = error;
    }

    public LoginResponse? Response { get; }
    public ApiError? Error { get; }

    public static LoginResult Success(LoginResponse response) => new(response, null);
    public static LoginResult Failure(ApiError error) => new(null, error);
}
