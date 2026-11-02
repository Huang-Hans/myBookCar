using LMS.BuildingBlocks.Core.Wire;
using LMS.Platform.Api.Contracts.Files;
using LMS.Platform.Application.Files.Models;
using LMS.Platform.Domain.Files;

namespace LMS.Platform.Api.Mapping.Files;

internal static class FileViewMapper
{
    // storageReference、propertyId、contentStoredAt 是内部字段，不进入响应。
    public static FileMetadataView ToView(FileMetadata file) => new(
        file.FileId,
        file.ApprovalId,
        file.DocumentType is null ? null : WireEnum.ToWire(file.DocumentType.Value),
        file.ExportId,
        file.OriginalFileName,
        file.ContentType,
        file.SizeBytes,
        file.Sha256,
        WireEnum.ToWire(file.ScanStatus),
        file.CreatedBy,
        file.CreatedAt.UtcDateTime);

    public static FileUploadDescriptionView ToView(FileRegistration registration) => new(
        registration.File.FileId,
        new UploadDescriptionPayload(
            registration.Upload.Url, registration.Upload.Method, registration.Upload.ContentType, registration.Upload.ExpiresAt.UtcDateTime));
}
