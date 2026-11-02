using LMS.Reporting.Domain.Files;
using LMS.Reporting.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Reporting.Infrastructure.Documents;

internal sealed class FileMetadataDocument
{
    [BsonId]
    public string FileId { get; init; } = string.Empty;

    [BsonElement("exportId")]
    public string ExportId { get; init; } = string.Empty;

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

    [BsonElement("createdBy")]
    public string CreatedBy { get; init; } = string.Empty;

    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; init; }

    public static FileMetadataDocument FromDomain(FileMetadata file) => new()
    {
        FileId = file.FileId,
        ExportId = file.ExportId,
        OriginalFileName = file.OriginalFileName,
        ContentType = file.ContentType,
        SizeBytes = file.SizeBytes,
        Sha256 = file.Sha256,
        StorageReference = file.StorageReference,
        CreatedBy = file.CreatedBy,
        CreatedAt = UtcDateTimes.ToStored(file.CreatedAt)
    };

    public FileMetadata ToDomain() => new(
        FileId, ExportId, OriginalFileName, ContentType, SizeBytes, Sha256, StorageReference, CreatedBy, UtcDateTimes.FromStored(CreatedAt));
}
