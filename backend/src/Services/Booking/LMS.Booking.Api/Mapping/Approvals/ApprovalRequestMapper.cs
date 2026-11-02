using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.Http.Requests;
using LMS.Booking.Api.Contracts.Approvals;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Approvals.Commands;
using LMS.Booking.Application.Approvals.Models;
using LMS.Booking.Domain.Approvals;
using LMS.Booking.Domain.Common;

namespace LMS.Booking.Api.Mapping.Approvals;

internal static class ApprovalRequestMapper
{
    public static ListApprovalsQuery ToListQuery(ApprovalListRequest request, PropertyScope scope) =>
        new(request.ToListQuery(), scope, request.PropertyId, request.Status, request.From, request.To);

    public static ListApprovalsQuery ToSummaryQuery(ApprovalSummaryRequest request, PropertyScope scope) =>
        new(new ListQuery(1, 1, request.Search, []), scope, request.PropertyId, request.Status, request.From, request.To);

    public static RequestParseResult<UpdateApprovalDocumentCommand> ToDocumentCommand(
        string approvalId,
        ApprovalDocumentType documentType,
        long expectedVersion,
        ApprovalDocumentPatchRequest request,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var changes = new ApprovalDocumentChanges(
            errors.PatchText("fileId", request.FileId),
            errors.PatchBool("isVerified", request.IsVerified));
        if (changes.ChangedFields().Count == 0)
        {
            errors.Add(string.Empty, "At least one field is required");
        }

        return errors.HasErrors
            ? RequestParseResult<UpdateApprovalDocumentCommand>.Fail(errors.ToFailure())
            : RequestParseResult<UpdateApprovalDocumentCommand>.Success(
                new UpdateApprovalDocumentCommand(approvalId, documentType, expectedVersion, changes, context));
    }

    public static RequestParseResult<RegisterApprovalUploadCommand> ToUploadCommand(
        string approvalId,
        ApprovalDocumentType documentType,
        FileUploadRequest request,
        string idempotencyKey,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var command = new RegisterApprovalUploadCommand(
            approvalId,
            documentType,
            errors.RequiredFileName("originalFileName", request.OriginalFileName),
            errors.RequiredText("contentType", request.ContentType),
            errors.RequiredPositiveLong("sizeBytes", request.SizeBytes),
            errors.RequiredSha256("sha256", request.Sha256),
            idempotencyKey,
            context);

        return errors.HasErrors
            ? RequestParseResult<RegisterApprovalUploadCommand>.Fail(errors.ToFailure())
            : RequestParseResult<RegisterApprovalUploadCommand>.Success(command);
    }

    public static RequestParseResult<DecideApprovalCommand> ToDecideCommand(
        string approvalId,
        long expectedVersion,
        ApprovalDecisionRequest request,
        string idempotencyKey,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var command = new DecideApprovalCommand(
            approvalId,
            expectedVersion,
            errors.RequiredEnum<ReviewDecision>("decision", request.Decision),
            errors.RequiredText("comments", request.Comments),
            idempotencyKey,
            context);

        return errors.HasErrors
            ? RequestParseResult<DecideApprovalCommand>.Fail(errors.ToFailure())
            : RequestParseResult<DecideApprovalCommand>.Success(command);
    }

    public static RequestParseResult<BulkDecideApprovalsCommand> ToBulkCommand(
        ApprovalBulkDecisionRequest request,
        string idempotencyKey,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var command = new BulkDecideApprovalsCommand(
            ReadItems(errors, request.Items),
            errors.RequiredEnum<ReviewDecision>("decision", request.Decision),
            errors.RequiredText("comments", request.Comments),
            idempotencyKey,
            context);

        return errors.HasErrors
            ? RequestParseResult<BulkDecideApprovalsCommand>.Fail(errors.ToFailure())
            : RequestParseResult<BulkDecideApprovalsCommand>.Success(command);
    }

    private static IReadOnlyList<BulkApprovalItem> ReadItems(FieldErrorCollector errors, List<ApprovalVersionItemRequest?>? requested)
    {
        if (requested is null || requested.Count == 0)
        {
            errors.Add("items", "Must contain at least one approval");
            return [];
        }

        var items = requested.Select((item, index) => ReadItem(errors, item, index)).ToList();
        if (items.GroupBy(item => item.ApprovalId, StringComparer.Ordinal).Any(group => group.Count() > 1))
        {
            errors.Add("items", "Each approval must appear at most once");
        }

        return items;
    }

    private static BulkApprovalItem ReadItem(FieldErrorCollector errors, ApprovalVersionItemRequest? item, int index)
    {
        if (item is null)
        {
            errors.Add($"items[{index}]", "Must not be null");
            return new BulkApprovalItem(string.Empty, 0);
        }

        var approvalId = errors.RequiredText($"items[{index}].approvalId", item.ApprovalId);
        // 乐观并发暂时取消：条目里的版本可省略，写入时以服务端读到的当前版本为准。
        var version = item.ExpectedVersion ?? 0;

        return new BulkApprovalItem(approvalId, version);
    }
}
