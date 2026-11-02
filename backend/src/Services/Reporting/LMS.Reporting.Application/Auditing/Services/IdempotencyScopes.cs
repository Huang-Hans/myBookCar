using LMS.Reporting.Application.Abstractions;
using LMS.Reporting.Application.Idempotency.Models;

namespace LMS.Reporting.Application.Auditing.Services;

internal static class IdempotencyScopes
{
    private const string ServiceName = "reporting";

    public static IdempotencyScope For(CommandContext context, string operation, string idempotencyKey) =>
        new(ServiceName, operation, context.Actor, idempotencyKey);
}
