namespace LMS.Booking.Api.Contracts.Customer;

/// <summary>
/// 客户取消订单的请求体。需要 Idempotency-Key 头；原因码由服务端配置映射，客户端不提交。
/// </summary>
/// <param name="Comments">取消备注，可选，最长 500 字符。</param>
public sealed record CancelCustomerTripRequest(string? Comments);
