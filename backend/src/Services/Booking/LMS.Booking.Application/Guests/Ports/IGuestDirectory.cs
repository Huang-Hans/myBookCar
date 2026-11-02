using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Booking.Domain.Guests;

namespace LMS.Booking.Application.Guests.Ports;

// 宾客读模型端口。一期实现读 lms_booking.guests（reference 快照），二期替换为 PMS/忠诚度客户端，端口不变。
public interface IGuestDirectory
{
    Task<PageResult<GuestProfile>> SearchAsync(ListQuery list, string? tierCode, PropertyScope scope, CancellationToken cancellationToken);

    Task<GuestProfile?> FindAsync(string guestId, CancellationToken cancellationToken);

    // 服务间导入/刷新快照（一期运维用；按 memberId 幂等 upsert）。
    Task UpsertAsync(GuestProfile guest, CancellationToken cancellationToken);
}
