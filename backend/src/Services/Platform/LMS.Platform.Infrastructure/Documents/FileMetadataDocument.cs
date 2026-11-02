using LMS.BuildingBlocks.Core.Wire;
using LMS.Platform.Domain.Files;
using LMS.Platform.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Documents;

internal sealed class FileMetadataDocument
{
    [BsonId]
    public string FileId { get; init; } = string.Empty;

    [BsonElement("approvalId")]
    public string? ApprovalId { get; init; }

    [BsonElement("documentType")]
    public string? DocumentType { get; init; }

    [BsonElement("exportId")]
    public string? ExportId { get; init; }

    [BsonElement("originalFileName")]
    public string OriginalFileName { get; init; } = string.Empty;

    [BsonElement("contentType")]
    public string ContentType { get; init; } = string.Empty;

    [BsonElement("sizeBytes")]
    public long SizeBytes { get; init; }

    [BsonElement("sha256")]
    public string Sha256 { get; init; } = string.Empty;

    // 内部引用，不进入任何响应。
    [BsonElement("storageReference")]
    public string StorageReference { get; init; } = string.Empty;

    [BsonElement("scanStatus")]
    public string ScanStatus { get; init; } = string.Empty;

    [BsonElement("createdBy")]
    public string CreatedBy { get; init; } = string.Empty;

    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; init; }

    // 设计扩展：所属审核的物业，仅用于访问范围判断；导出结果文件为 null。
    [BsonElement("propertyId")]
    public string? PropertyId { get; init; }

    // 设计扩展：内容写入完成的时间；null 表示内容尚未上传。
    [BsonElement("contentStoredAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? ContentStoredAt { get; init; }

    public static FileMetadataDocument FromDomain(FileMetadata file) => new()
    {
        FileId = file.FileId,
        ApprovalId = file.ApprovalId,
        DocumentType = file.DocumentType is null ? null : WireEnum.ToWire(file.DocumentType.Value),
        ExportId = file.ExportId,
        OriginalFileName = file.OriginalFileName,
        ContentType = file.ContentType,
        SizeBytes = file.SizeBytes,
        Sha256 = file.Sha256,
        StorageReference = file.StorageReference,
        ScanStatus = WireEnum.ToWire(file.ScanStatus),
        CreatedBy = file.CreatedBy,
        CreatedAt = UtcDateTimes.ToStored(file.CreatedAt),
        PropertyId = file.PropertyId,
        ContentStoredAt = file.ContentStoredAt is null ? null : UtcDateTimes.ToStored(file.ContentStoredAt.Value)
    };

    public FileMetadata ToDomain() => new(
        FileId,
        ApprovalId,
        DocumentType is null ? null : StoredValues.ParseEnum<FileDocumentType>(DocumentType, "documentType"),
        ExportId,
        OriginalFileName,
        ContentType,
        SizeBytes,
        Sha256,
        StorageReference,
        StoredValues.ParseEnum<ScanStatus>(ScanStatus, "scanStatus"),
        CreatedBy,
        UtcDateTimes.FromStored(CreatedAt),
        PropertyId,
        ContentStoredAt is null ? null : UtcDateTimes.FromStored(ContentStoredAt.Value));
}
