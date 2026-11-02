using LMS.BuildingBlocks.Http.Requests;
using LMS.Platform.Api.Contracts.CustomerBlacklists;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.CustomerBlacklists.Commands;
using LMS.Platform.Application.CustomerBlacklists.Models;
using LMS.Platform.Domain.CustomerBlacklists;

namespace LMS.Platform.Api.Mapping.CustomerBlacklists;

internal static class CustomerBlacklistRequestMapper
{
    public static ListCustomerBlacklistsQuery ToListQuery(CustomerBlacklistListRequest request) =>
        new(request.ToListQuery(), request.IsActive);

    public static RequestParseResult<CreateCustomerBlacklistCommand> ToCreateCommand(
        CreateCustomerBlacklistRequest request,
        string idempotencyKey,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var command = new CreateCustomerBlacklistCommand(
            errors.RequiredText("customerName", request.CustomerName),
            errors.RequiredText("memberId", request.MemberId),
            errors.RequiredText("reason", request.Reason),
            errors.RequiredDate("effectiveDate", request.EffectiveDate),
            errors.RequiredDate("expiryDate", request.ExpiryDate),
            errors.RequiredBool("isActive", request.IsActive),
            idempotencyKey,
            context);

        return errors.HasErrors
            ? RequestParseResult<CreateCustomerBlacklistCommand>.Fail(errors.ToFailure())
            : RequestParseResult<CreateCustomerBlacklistCommand>.Success(command);
    }

    public static RequestParseResult<UpdateCustomerBlacklistCommand> ToUpdateCommand(
        string blacklistId,
        long expectedVersion,
        PatchCustomerBlacklistRequest request,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var changes = new CustomerBlacklistChanges(
            errors.PatchText("customerName", request.CustomerName),
            errors.PatchText("memberId", request.MemberId),
            errors.PatchText("reason", request.Reason),
            errors.PatchDate("effectiveDate", request.EffectiveDate),
            errors.PatchDate("expiryDate", request.ExpiryDate),
            errors.PatchBool("isActive", request.IsActive));
        if (changes.ChangedFields().Count == 0)
        {
            errors.Add(string.Empty, "At least one field is required");
        }

        return errors.HasErrors
            ? RequestParseResult<UpdateCustomerBlacklistCommand>.Fail(errors.ToFailure())
            : RequestParseResult<UpdateCustomerBlacklistCommand>.Success(
                new UpdateCustomerBlacklistCommand(blacklistId, expectedVersion, changes, context));
    }
}
