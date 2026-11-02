namespace LMS.Reporting.Domain.Files;

// 导出结果文件的元数据。StorageReference 是内部引用，禁止返回客户端。结果由本服务生成，内容写入后一并登记。
public sealed record FileMetadata(
    string FileId,
    string ExportId,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string Sha256,
    string StorageReference,
    string CreatedBy,
    DateTimeOffset CreatedAt)
{
    private const string IdPrefix = "file_";

    public static FileMetadata ForExportResult(
        string storageToken,
        string exportId,
        string originalFileName,
        string contentType,
        long sizeBytes,
        string sha256,
        string createdBy,
        DateTimeOffset createdAt) =>
        new(IdPrefix + storageToken, exportId, originalFileName, contentType, sizeBytes, sha256, storageToken, createdBy, createdAt);
}
