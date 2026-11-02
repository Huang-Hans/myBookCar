using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Requests;
using LMS.Booking.Api.Contracts.Overrides;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Overrides.Commands;
using LMS.Booking.Application.Overrides.Models;
using LMS.Booking.Domain.Common;
using LMS.Booking.Domain.Overrides;

namespace LMS.Booking.Api.Mapping.Overrides;

internal static class OverrideMapper
{
    public static ListQuotaOverridesQuery ToListQuery(QuotaOverrideListRequest request, PropertyScope scope) =>
        new(request.ToListQuery(), scope, request.PropertyId);

    public static QuotaOverrideView ToView(QuotaOverride quotaOverride) => new(
        quotaOverride.OverrideId,
        quotaOverride.PropertyId,
        quotaOverride.GuestId,
        quotaOverride.AdditionalDailyEntitlements,
        quotaOverride.Justification,
        WireEnum.ToWire(quotaOverride.Status),
        quotaOverride.RequestedBy,
        quotaOverride.RequestedAt.UtcDateTime,
        quotaOverride.Decision is null
            ? null
            : new OverrideDecisionView(
                WireEnum.ToWire(quotaOverride.Decision.Decision), quotaOverride.Decision.DecidedBy, quotaOverride.Decision.DecidedAt.UtcDateTime),
        quotaOverride.Version);

    public static RequestParseResult<CreateQuotaOverrideCommand> ToCreateCommand(
        CreateQuotaOverrideRequest request,
        string idempotencyKey,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var command = new CreateQuotaOverrideCommand(
            errors.RequiredText("propertyId", request.PropertyId),
            errors.RequiredText("guestId", request.GuestId),
            errors.RequiredInt("additionalDailyEntitlements", request.AdditionalDailyEntitlements),
            errors.RequiredText("justification", request.Justification),
            idempotencyKey,
            context);

        return errors.HasErrors
            ? RequestParseResult<CreateQuotaOverrideCommand>.Fail(errors.ToFailure())
            : RequestParseResult<CreateQuotaOverrideCommand>.Success(command);
    }

    public static RequestParseResult<DecideQuotaOverrideCommand> ToDecideCommand(
        string overrideId,
        long expectedVersion,
        QuotaOverrideDecisionRequest request,
        string idempotencyKey,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var decision = errors.RequiredEnum<ReviewDecision>("decision", request.Decision);

        return errors.HasErrors
            ? RequestParseResult<DecideQuotaOverrideCommand>.Fail(errors.ToFailure())
            : RequestParseResult<DecideQuotaOverrideCommand>.Success(
                new DecideQuotaOverrideCommand(overrideId, expectedVersion, decision, idempotencyKey, context));
    }
}
