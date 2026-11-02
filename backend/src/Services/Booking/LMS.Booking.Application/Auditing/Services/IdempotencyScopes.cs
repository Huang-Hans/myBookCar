using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Idempotency.Models;

namespace LMS.Booking.Application.Auditing.Services;

internal static class IdempotencyScopes
{
    private const string ServiceName = "booking";

    public static IdempotencyScope For(CommandContext context, string operation, string idempotencyKey) =>
        new(ServiceName, operation, context.Actor, idempotencyKey);
}
