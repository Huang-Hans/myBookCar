using LMS.Booking.Application.Abstractions;

namespace LMS.Booking.Application.Reviews.Commands;

// 客户对本人已完成行程创建 / 修改评价。GuestId 来自令牌；Comments 已去除首尾空白，空白为 null。
// 修改已有评价必须带期望版本（ExpectedVersion 大于 0）；内容与已保存的相同则无需版本，直接返回已保存的评价。
public sealed record UpsertTripReviewCommand(
    string TripId,
    string GuestId,
    int Rating,
    string? Comments,
    long ExpectedVersion,
    CommandContext Context);
