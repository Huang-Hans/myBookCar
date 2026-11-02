using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.Idempotency.Models;

namespace LMS.Platform.Application.Auditing.Services;

internal static class IdempotencyScopes
{
    private const string ServiceName = "platform";

    public static IdempotencyScope For(CommandContext context, string operation, string idempotencyKey) =>
        new(ServiceName, operation, context.Actor, idempotencyKey);
}
