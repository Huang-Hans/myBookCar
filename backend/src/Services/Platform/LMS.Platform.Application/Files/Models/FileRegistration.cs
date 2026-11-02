using LMS.Platform.Domain.Files;

namespace LMS.Platform.Application.Files.Models;

public sealed record FileRegistration(FileMetadata File, UploadDescription Upload);
