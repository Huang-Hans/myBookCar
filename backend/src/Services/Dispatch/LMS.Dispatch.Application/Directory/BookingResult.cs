namespace LMS.Dispatch.Application.Directory;

// 对 Booking 的服务间调用结果。
public sealed record BookingResult<T>(BookingOutcome Outcome, T? Value)
{
    public bool IsSuccess => Outcome == BookingOutcome.Success;

    public static BookingResult<T> Success(T value) => new(BookingOutcome.Success, value);

    public static BookingResult<T> Failed(BookingOutcome outcome) => new(outcome, default);
}
