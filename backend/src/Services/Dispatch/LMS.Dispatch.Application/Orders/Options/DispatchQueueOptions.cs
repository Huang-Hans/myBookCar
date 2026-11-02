namespace LMS.Dispatch.Application.Orders.Options;

// 派单队列设置（DispatchQueue 节）。这些是安全边界，不是业务规则。
public sealed class DispatchQueueOptions
{
    public const string SectionName = "DispatchQueue";

    // 只有这段时间内创建的邀请会影响队列状态：更早的邀请不再决定订单是“需重派”还是“未派单”。
    public int OfferLookbackDays { get; init; } = 30;

    // 按邀请状态筛选或计数时允许携带给 Booking 的订单集合上限，需不大于 Booking 接受的上限。
    public int MaxStatusSetSize { get; init; } = 5000;

    public TimeSpan OfferLookback => TimeSpan.FromDays(Math.Max(1, OfferLookbackDays));
}
