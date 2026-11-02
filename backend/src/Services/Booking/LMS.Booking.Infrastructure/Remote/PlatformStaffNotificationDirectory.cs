using System.Net.Http.Json;
using LMS.BuildingBlocks.Core.Authorization;
using LMS.Booking.Application.Directory;
using LMS.Booking.Application.Notifications.Ports;
using LMS.Booking.Infrastructure.ServiceIdentity;

namespace LMS.Booking.Infrastructure.Remote;

// 通过 Platform 的内部受限接口取得物业范围内的授权客服。使用 Booking 自己的服务身份令牌（只含收件人查询范围），
// 不使用任何终端用户令牌。服务身份未配置、Platform 不可用或目录未配置收件人角色时都按“目录不可用”处理，
// 绝不把失败当作零收件人。
internal sealed class PlatformStaffNotificationDirectory(HttpClient client, IServiceTokenIssuer tokens) : IStaffNotificationDirectory
{
    public async Task<DirectoryResult<IReadOnlyList<string>>> ListRecipientsAsync(string eventType, string propertyId, CancellationToken cancellationToken)
    {
        if (!tokens.IsAvailable)
        {
            return DirectoryResult<IReadOnlyList<string>>.Failed(DirectoryOutcome.Unavailable);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "internal/v1/notification-recipients/queries")
        {
            Content = JsonContent.Create(new { eventType, propertyId })
        };
        var response = await RemoteHttp.SendAsync(client, request, tokens.Issue(ServiceScopes.NotificationRecipientsQuery), cancellationToken);

        // 非成功（含 401/403/400 这类配置问题）一律不是“零收件人”，调用方按失败重试。
        return await RemoteHttp.ReadAsync<RemoteNotificationRecipientsPayload, IReadOnlyList<string>>(
            response, payload => payload.Recipients.Select(recipient => recipient.UserId).ToList(), cancellationToken);
    }
}
