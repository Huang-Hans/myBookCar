using Microsoft.AspNetCore.Http;

namespace LMS.BuildingBlocks.Http.Requests;

public sealed class RequestParseResult<T> : IRequestParseResult
{
    private readonly T? value;

    private RequestParseResult(T? value, IResult? failure)
    {
        this.value = value;
        Failure = failure;
    }

    public IResult? Failure { get; }

    public T Value => Failure is null
        ? value!
        : throw new InvalidOperationException("The request part failed to parse and has no value.");

    public static RequestParseResult<T> Success(T value) => new(value, null);

    public static RequestParseResult<T> Fail(IResult failure) => new(default, failure);
}
