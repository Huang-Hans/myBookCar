namespace LMS.BuildingBlocks.Http.Contracts;

// 当前使用的应用码，登记于 docs/api/error-codes.md。
public static class ApiCodes
{
    public const int Success = 20000;
    public const int Created = 20100;
    public const int Accepted = 20200;
    public const int MalformedRequest = 40000;
    public const int ValidationFailed = 40001;
    public const int NotAuthenticated = 40100;
    public const int TokenExpired = 40101;
    public const int InsufficientPermission = 40300;
    public const int ResourceNotFound = 40400;
    public const int BusinessRuleRejected = 42200;
    public const int StateConflict = 40900;
    public const int TooManyRequests = 42900;
    public const int InternalError = 50000;
    public const int BadGateway = 50200;
    public const int ServiceUnavailable = 50300;
    public const int GatewayTimeout = 50400;
}
