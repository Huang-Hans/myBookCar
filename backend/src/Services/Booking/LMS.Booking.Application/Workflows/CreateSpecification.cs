using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Idempotency.Models;
using LMS.Booking.Domain.Auditing;

namespace LMS.Booking.Application.Workflows;

// 一次“带幂等键的创建”所需的全部差异点；通用流程由 CreateWorkflow 执行。
internal sealed record CreateSpecification<TResource>(
    IdempotencyScope Scope,
    string RequestCanonicalForm,
    Func<ITransactionContext, CancellationToken, Task<TResource>> Build,
    Func<TResource, string> IdOf,
    Func<TResource, ITransactionContext, CancellationToken, Task> Insert,
    Func<string, CancellationToken, Task<TResource?>> Load,
    Func<TResource, DateTimeOffset, AuditRecord> BuildAudit);
