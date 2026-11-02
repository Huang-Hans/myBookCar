using LMS.Dispatch.Application.Offers.Models;
using LMS.Dispatch.Domain.Offers;

namespace LMS.Dispatch.Application.Orders.Models;

// 按最近一轮邀请把订单分成三个集合：等待响应、处理中、需重派。它们互斥，与 DispatchStatusRules 对已确认订单的判定口径一致。
internal sealed record OfferStateIndex(
    IReadOnlyList<string> Awaiting,
    IReadOnlyList<string> Processing,
    IReadOnlyList<string> Redispatch)
{
    public int Size => Awaiting.Count + Processing.Count + Redispatch.Count;

    public IReadOnlyList<string> All => [.. Awaiting, .. Processing, .. Redispatch];

    public static OfferStateIndex Build(IReadOnlyList<OfferState> states, DateTimeOffset now) => new(
        IdsOf(states, state => state.Status == DriverOfferStatus.Pending && state.ExpiresAt > now),
        IdsOf(states, state => state.Status == DriverOfferStatus.Accepting),
        IdsOf(states, state => IsClosedWithoutAssignment(state, now)));

    private static bool IsClosedWithoutAssignment(OfferState state, DateTimeOffset now) => state.Status switch
    {
        DriverOfferStatus.Pending => state.ExpiresAt <= now,
        DriverOfferStatus.Declined or DriverOfferStatus.Expired or DriverOfferStatus.Superseded => true,
        _ => false
    };

    private static List<string> IdsOf(IReadOnlyList<OfferState> states, Func<OfferState, bool> predicate) =>
        states.Where(predicate).Select(state => state.TripId).ToList();
}
