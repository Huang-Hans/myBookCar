using LMS.BuildingBlocks.Http.Requests;
using LMS.Platform.Api.Contracts.Files;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.Files.Commands;
using LMS.Platform.Domain.Files;

namespace LMS.Platform.Api.Mapping.Files;

internal static class FileRequestMapper
{
    public static RequestParseResult<RegisterFileCommand> ToCommand(
        RegisterFileInternalRequest request,
        string idempotencyKey,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var command = new RegisterFileCommand(
            errors.RequiredText("approvalId", request.ApprovalId),
            errors.RequiredEnum<FileDocumentType>("documentType", request.DocumentType),
            errors.RequiredText("propertyId", request.PropertyId),
            errors.RequiredFileName("originalFileName", request.OriginalFileName),
            errors.RequiredText("contentType", request.ContentType),
            errors.RequiredPositiveLong("sizeBytes", request.SizeBytes),
            errors.RequiredSha256("sha256", request.Sha256),
            idempotencyKey,
            context);

        return errors.HasErrors
            ? RequestParseResult<RegisterFileCommand>.Fail(errors.ToFailure())
            : RequestParseResult<RegisterFileCommand>.Success(command);
    }
}
