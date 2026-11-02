using System.Net.Http.Json;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Directory;

namespace LMS.Booking.Infrastructure.Remote;

// 通过 Dispatch 的服务间端点读取司机状态，转发终端用户的令牌，由 Dispatch 按其物业范围限定结果。
internal sealed class DriverDirectory(HttpClient client, ICallerToken callerToken) : IDriverDirectory
{
    public async Task<DirectoryResult<IReadOnlyList<DriverStatusSnapshot>>> GetStatusesAsync(
        IReadOnlyList<string> driverIds,
        CancellationToken cancellationToken)
    {
        if (driverIds.Count == 0)
        {
            return DirectoryResult<IReadOnlyList<DriverStatusSnapshot>>.Success([]);
        }

        if (callerToken.Value is null)
        {
            return DirectoryResult<IReadOnlyList<DriverStatusSnapshot>>.Failed(DirectoryOutcome.Unavailable);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "internal/v1/driver-status-checks")
        {
            Content = JsonContent.Create(new { driverIds })
        };

        return await RemoteHttp.ReadAsync<List<RemoteDriverStatusPayload>, IReadOnlyList<DriverStatusSnapshot>>(
            await RemoteHttp.SendAsync(client, request, callerToken.Value, cancellationToken),
            payload => payload.Select(ToSnapshot).ToList(),
            cancellationToken);
    }

    private static DriverStatusSnapshot ToSnapshot(RemoteDriverStatusPayload payload) =>
        WireEnum.TryParse<DirectoryDriverStatus>(payload.Status, out var status)
            ? new DriverStatusSnapshot(payload.DriverId, payload.PropertyId, status, payload.RestrictionReason)
            : throw new InvalidDataException($"Unknown driver status '{payload.Status}'.");
}
