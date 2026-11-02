using LMS.Booking.Application.Abstractions;
using LMS.Booking.Domain.Staff;

namespace LMS.Booking.Application.Staff.Ports;

public interface IStaffPreferencesRepository
{
    Task<StaffPreferences?> FindAsync(string userId, CancellationToken cancellationToken);

    // 整体替换（不存在则插入）。带事务上下文的重载供建单流程在同事务内记录最近地点。
    Task UpsertAsync(StaffPreferences preferences, CancellationToken cancellationToken);

    Task UpsertAsync(StaffPreferences preferences, ITransactionContext context, CancellationToken cancellationToken);
}
