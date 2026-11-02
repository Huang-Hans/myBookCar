using System.Net;
using System.Net.Http.Json;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Directory;
using LMS.Booking.Domain.Approvals;

namespace LMS.Booking.Infrastructure.Remote;

// 通过 Platform 的服务间端点解析调用者授权、核对物业与客户限制、登记并读取审核材料文件。
// 转发的是终端用户自己的令牌，Platform 因此会重新检查该用户的权限与物业范围；Booking 不持有额外的服务凭据。
internal sealed class PlatformDirectory(HttpClient client, ICallerToken callerToken) : IPlatformDirectory
{
    public async Task<CallerLookup> ResolveCallerAsync(CancellationToken cancellationToken)
    {
        if (callerToken.Value is null)
        {
            return new CallerLookup(CallerLookupOutcome.NotLinked, null);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "internal/v1/caller-authorization");
        var result = await RemoteHttp.ReadAsync<RemoteCallerPayload, PlatformCaller>(
            await RemoteHttp.SendAsync(client, request, callerToken.Value, cancellationToken),
            payload => new PlatformCaller(payload.UserId, payload.RoleCode, payload.HasAllPropertyAccess, payload.PropertyId, payload.Permissions, payload.Name),
            cancellationToken);

        return result.Outcome switch
        {
            DirectoryOutcome.Success => new CallerLookup(CallerLookupOutcome.Found, result.Value),
            DirectoryOutcome.Forbidden => new CallerLookup(CallerLookupOutcome.NotLinked, null),
            _ => new CallerLookup(CallerLookupOutcome.SourceUnavailable, null)
        };
    }

    public async Task<PropertyLookup> ContainsPropertyAsync(string propertyId, CancellationToken cancellationToken)
    {
        if (callerToken.Value is null)
        {
            return PropertyLookup.SourceUnavailable;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"internal/v1/properties/{Uri.EscapeDataString(propertyId)}");
        using var response = await RemoteHttp.SendAsync(client, request, callerToken.Value, cancellationToken);

        return response?.StatusCode switch
        {
            HttpStatusCode.NoContent => PropertyLookup.Found,
            HttpStatusCode.NotFound => PropertyLookup.NotFound,
            _ => PropertyLookup.SourceUnavailable
        };
    }

    public async Task<DirectoryResult<IReadOnlyList<CustomerRestrictionInfo>>> CheckCustomersAsync(
        IReadOnlyList<string> memberIds,
        CancellationToken cancellationToken)
    {
        if (memberIds.Count == 0)
        {
            return DirectoryResult<IReadOnlyList<CustomerRestrictionInfo>>.Success([]);
        }

        if (callerToken.Value is null)
        {
            return DirectoryResult<IReadOnlyList<CustomerRestrictionInfo>>.Failed(DirectoryOutcome.Unavailable);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "internal/v1/customer-restriction-checks")
        {
            Content = JsonContent.Create(new { memberIds })
        };

        return await RemoteHttp.ReadAsync<List<RemoteRestrictionPayload>, IReadOnlyList<CustomerRestrictionInfo>>(
            await RemoteHttp.SendAsync(client, request, callerToken.Value, cancellationToken),
            payload => payload.Select(item => new CustomerRestrictionInfo(item.MemberId, item.Reason)).ToList(),
            cancellationToken);
    }

    public async Task<DirectoryResult<RegisteredFile>> GetFileAsync(string fileId, CancellationToken cancellationToken)
    {
        if (callerToken.Value is null)
        {
            return DirectoryResult<RegisteredFile>.Failed(DirectoryOutcome.Unavailable);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"internal/v1/files/{Uri.EscapeDataString(fileId)}");

        return await RemoteHttp.ReadAsync<RemoteFilePayload, RegisteredFile>(
            await RemoteHttp.SendAsync(client, request, callerToken.Value, cancellationToken), ToRegisteredFile, cancellationToken);
    }

    public async Task<DirectoryResult<FileUploadInfo>> RegisterFileAsync(
        RegisterFileRequest registration,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (callerToken.Value is null)
        {
            return DirectoryResult<FileUploadInfo>.Failed(DirectoryOutcome.Unavailable);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "internal/v1/files") { Content = JsonContent.Create(ToBody(registration)) };
        request.Headers.Add("Idempotency-Key", idempotencyKey);

        return await RemoteHttp.ReadAsync<RemoteFileUploadPayload, FileUploadInfo>(
            await RemoteHttp.SendAsync(client, request, callerToken.Value, cancellationToken), ToUploadInfo, cancellationToken);
    }

    private static object ToBody(RegisterFileRequest registration) => new
    {
        approvalId = registration.ApprovalId,
        documentType = WireEnum.ToWire(registration.DocumentType),
        propertyId = registration.PropertyId,
        originalFileName = registration.OriginalFileName,
        contentType = registration.ContentType,
        sizeBytes = registration.SizeBytes,
        sha256 = registration.Sha256
    };

    private static RegisteredFile ToRegisteredFile(RemoteFilePayload payload) => new(
        payload.FileId,
        payload.ApprovalId,
        ParseDocumentType(payload.DocumentType),
        Parse<FileScanStatus>(payload.ScanStatus));

    private static ApprovalDocumentType? ParseDocumentType(string? text) => text is null ? null : Parse<ApprovalDocumentType>(text);

    private static T Parse<T>(string text) where T : struct, Enum =>
        WireEnum.TryParse<T>(text, out var value) ? value : throw new InvalidDataException($"Unknown value '{text}'.");

    private static FileUploadInfo ToUploadInfo(RemoteFileUploadPayload payload) => new(
        payload.FileId,
        payload.Upload.Url,
        payload.Upload.Method,
        payload.Upload.ContentType,
        new DateTimeOffset(DateTime.SpecifyKind(payload.Upload.ExpiresAt, DateTimeKind.Utc)));
}
