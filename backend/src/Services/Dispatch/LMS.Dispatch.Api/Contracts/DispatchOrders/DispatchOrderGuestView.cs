namespace LMS.Dispatch.Api.Contracts.DispatchOrders;

/// <summary>派单页面的宾客摘要。</summary>
/// <param name="Name">宾客姓名。</param>
/// <param name="RoomNumber">房号；没有时省略。</param>
/// <param name="TierCode">会员等级代码。</param>
/// <param name="MemberId">会员号；没有时省略。</param>
public sealed record DispatchOrderGuestView(string Name, string? RoomNumber, string TierCode, string? MemberId);
