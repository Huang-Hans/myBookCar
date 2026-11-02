namespace LMS.Platform.Domain.Files;

// 文件元数据。StorageReference 是内部引用，禁止返回客户端；PropertyId 是登记时所属审核的物业，
// 只用于访问范围判断（不对外输出）；ContentStoredAt 为空表示内容尚未上传。
// Sha256 在登记时是声明值，内容上传后由服务端核实，不一致则状态置为 FAILED。
public sealed record FileMetadata(
    string FileId,
    string? ApprovalId,
    FileDocumentType? DocumentType,
    string? ExportId,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string Sha256,
    string StorageReference,
    ScanStatus ScanStatus,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    string? PropertyId,
    DateTimeOffset? ContentStoredAt)
{
    private const string IdPrefix = "file_";

    // 导出结果文件：由服务端自己生成，内容写入后一并登记；没有审核、材料类型与物业归属。
    public static FileMetadata ForExportResult(
        string storageToken,
        string exportId,
        string originalFileName,
        string contentType,
        long sizeBytes,
        string sha256,
        string createdBy,
        DateTimeOffset createdAt) =>
        new(IdPrefix + storageToken, null, null, exportId, originalFileName, contentType, sizeBytes, sha256,
            storageToken, ScanStatus.Clean, createdBy, createdAt, null, createdAt);

    public static FileMetadata ForApprovalDocument(
        string approvalId,
        FileDocumentType documentType,
        string propertyId,
        string originalFileName,
        string contentType,
        long sizeBytes,
        string sha256,
        string createdBy,
        DateTimeOffset createdAt)
    {
        var token = Guid.CreateVersion7().ToString("N");

        return new FileMetadata(
            IdPrefix + token, approvalId, documentType, null, originalFileName, contentType, sizeBytes, sha256,
            token, ScanStatus.Quarantined, createdBy, createdAt, propertyId, null);
    }
}
