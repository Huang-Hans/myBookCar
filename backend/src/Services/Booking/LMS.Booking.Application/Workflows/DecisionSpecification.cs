using LMS.Booking.Application.Idempotency.Models;

namespace LMS.Booking.Application.Workflows;

// 一次“带幂等键和期望版本的决定”：按版本局部更新资源，同时登记幂等记录。
// 重放时按 resourceId 取资源当前状态（Q09 未决：不是首次响应的原文副本）。
internal sealed record DecisionSpecification<TResource>(
    IdempotencyScope Scope,
    string RequestCanonicalForm,
    PatchSpecification<TResource> Patch,
    Func<string, CancellationToken, Task<TResource?>> Load);
