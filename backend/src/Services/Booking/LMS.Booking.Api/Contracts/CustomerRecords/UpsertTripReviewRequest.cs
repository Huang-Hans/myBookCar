namespace LMS.Booking.Api.Contracts.CustomerRecords;

/// <summary>
/// 创建或修改评价的请求体（PUT，完整写入 rating 与 comments）。首次创建 201；内容与已保存的相同返回已保存的评价（200）；
/// 修改已有评价必须带 X-Expected-Version，否则或不一致为 409。客户和司机都不由请求给出。
/// </summary>
/// <param name="Rating">评分，整数 1～5。</param>
/// <param name="Comments">评语，可空，首尾空白会被去除，最长 1000 个字符（按 Unicode 字符计）。</param>
public sealed record UpsertTripReviewRequest(int? Rating, string? Comments);
