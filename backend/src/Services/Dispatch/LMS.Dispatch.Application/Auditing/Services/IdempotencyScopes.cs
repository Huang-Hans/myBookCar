using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Idempotency.Models;

namespace LMS.Dispatch.Application.Auditing.Services;

internal static class IdempotencyScopes
{
    private const string ServiceName = "dispatch";

    public static IdempotencyScope For(CommandContext context, string operation, string idempotencyKey) =>
        new(ServiceName, operation, context.Actor, idempotencyKey);
}
