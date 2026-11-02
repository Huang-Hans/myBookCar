using System.Text.Json;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Domain.Files;

namespace LMS.Platform.Application.Files.Commands;

// 为某个审核的某种材料登记文件。PropertyId 取自该审核（由 Booking 在其授权范围内确认），用于文件的访问范围。
public sealed record RegisterFileCommand(
    string ApprovalId,
    FileDocumentType DocumentType,
    string PropertyId,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string Sha256,
    string IdempotencyKey,
    CommandContext Context)
{
    public string ToCanonicalForm() =>
        JsonSerializer.Serialize(new object?[]
        {
            ApprovalId, WireEnum.ToWire(DocumentType), PropertyId, OriginalFileName, ContentType, SizeBytes, Sha256
        });
}
