using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Requests;
using LMS.Booking.Api.Contracts.Quotas;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Quotas.Commands;
using LMS.Booking.Application.Quotas.Models;
using LMS.Booking.Domain.Quotas;

namespace LMS.Booking.Api.Mapping.Quotas;

internal static class QuotaMapper
{
    public static ListPropertyQuotasQuery ToListQuery(PropertyQuotaListRequest request, PropertyScope scope) =>
        new(request.ToListQuery(), scope, request.PropertyId);

    public static PropertyQuotaView ToView(PropertyQuota quota) => new(
        quota.PropertyId,
        quota.DailyQuota,
        quota.WeeklyQuota,
        quota.MonthlyQuota,
        WireEnum.ToWire(quota.ExhaustionPolicy),
        quota.DailyUsed,
        quota.WeeklyUsed,
        quota.MonthlyUsed,
        quota.Version);

    public static DailyQuotaView ToView(DailyQuotaInfo info) => new(info.PropertyId, info.DailyQuota);

    public static RequestParseResult<UpdatePropertyQuotaCommand> ToUpdateCommand(
        string propertyId,
        long expectedVersion,
        PatchPropertyQuotaRequest request,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var changes = new PropertyQuotaChanges(
            errors.PatchInt("dailyQuota", request.DailyQuota),
            errors.PatchInt("weeklyQuota", request.WeeklyQuota),
            errors.PatchInt("monthlyQuota", request.MonthlyQuota),
            errors.PatchEnum<ExhaustionPolicy>("exhaustionPolicy", request.ExhaustionPolicy));
        if (changes.ChangedFields().Count == 0)
        {
            errors.Add(string.Empty, "At least one field is required");
        }

        return errors.HasErrors
            ? RequestParseResult<UpdatePropertyQuotaCommand>.Fail(errors.ToFailure())
            : RequestParseResult<UpdatePropertyQuotaCommand>.Success(new UpdatePropertyQuotaCommand(propertyId, expectedVersion, changes, context));
    }
}
