using System.Collections.Frozen;
using LMS.BuildingBlocks.Http.Contracts;
using Microsoft.AspNetCore.Http;

namespace LMS.BuildingBlocks.Http.Errors;

public static class ApiErrors
{
    public static readonly ApiError MalformedRequest =
        new(StatusCodes.Status400BadRequest, ApiCodes.MalformedRequest, "Malformed request");

    public static readonly ApiError ValidationFailed =
        new(StatusCodes.Status400BadRequest, ApiCodes.ValidationFailed, "Validation failed");

    public static readonly ApiError NotAuthenticated =
        new(StatusCodes.Status401Unauthorized, ApiCodes.NotAuthenticated, "Not authenticated");

    public static readonly ApiError TokenExpired =
        new(StatusCodes.Status401Unauthorized, ApiCodes.TokenExpired, "Token expired");

    public static readonly ApiError InsufficientPermission =
        new(StatusCodes.Status403Forbidden, ApiCodes.InsufficientPermission, "Insufficient permission");

    public static readonly ApiError ResourceNotFound =
        new(StatusCodes.Status404NotFound, ApiCodes.ResourceNotFound, "Resource not found");

    public static readonly ApiError StateConflict =
        new(StatusCodes.Status409Conflict, ApiCodes.StateConflict, "State conflict");

    public static readonly ApiError BusinessRuleRejected =
        new(StatusCodes.Status422UnprocessableEntity, ApiCodes.BusinessRuleRejected, "Business rule rejected");

    public static readonly ApiError TooManyRequests =
        new(StatusCodes.Status429TooManyRequests, ApiCodes.TooManyRequests, "Too many requests");

    public static readonly ApiError InternalError =
        new(StatusCodes.Status500InternalServerError, ApiCodes.InternalError, "Internal server error");

    public static readonly ApiError ServiceUnavailable =
        new(StatusCodes.Status503ServiceUnavailable, ApiCodes.ServiceUnavailable, "Service unavailable");

    private static readonly FrozenDictionary<int, ApiError> ErrorsByStatusCode = new[]
    {
        MalformedRequest,
        NotAuthenticated,
        InsufficientPermission,
        ResourceNotFound,
        StateConflict,
        BusinessRuleRejected,
        TooManyRequests,
        InternalError,
        ServiceUnavailable,
        new ApiError(StatusCodes.Status405MethodNotAllowed, ApiCodes.MalformedRequest, "Method not allowed"),
        new ApiError(StatusCodes.Status415UnsupportedMediaType, ApiCodes.MalformedRequest, "Unsupported media type"),
        new ApiError(StatusCodes.Status502BadGateway, ApiCodes.BadGateway, "Bad gateway"),
        new ApiError(StatusCodes.Status504GatewayTimeout, ApiCodes.GatewayTimeout, "Gateway timeout"),
    }.ToFrozenDictionary(error => error.StatusCode);

    public static ApiError FromStatusCode(int statusCode)
    {
        if (ErrorsByStatusCode.TryGetValue(statusCode, out var error))
        {
            return error;
        }

        return statusCode < StatusCodes.Status500InternalServerError
            ? new ApiError(statusCode, ApiCodes.MalformedRequest, "Request rejected")
            : new ApiError(statusCode, ApiCodes.InternalError, InternalError.Message);
    }

    public static ApiError FromException(Exception exception) =>
        exception is BadHttpRequestException badRequest ? FromStatusCode(badRequest.StatusCode) : InternalError;
}
