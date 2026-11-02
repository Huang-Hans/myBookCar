using System.Text.Json;
using LMS.Dispatch.Application.Abstractions;

namespace LMS.Dispatch.Application.Offers.Commands;

// 可信调度来源为指定行程向指定司机创建一轮邀请。
// ExpectedRound 为调度员看到的最近一轮轮次（来自 X-Expected-Version）；0 表示不比较。
public sealed record CreateDriverOfferCommand(string TripId, string DriverId, string IdempotencyKey, long ExpectedRound, CommandContext Context)
{
    public string ToCanonicalForm() => JsonSerializer.Serialize(new object?[] { TripId, DriverId });
}
