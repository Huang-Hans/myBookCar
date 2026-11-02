namespace LMS.BuildingBlocks.Http.Contracts;

public sealed record ApiError(int StatusCode, int Code, string Message);
