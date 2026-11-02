namespace LMS.Booking.Application.Directory;

// Dispatch 拥有司机主档；Booking 只读取状态，不直接访问 Dispatch 的数据库。
public interface IDriverDirectory
{
    // 只返回对调用者可见的司机；不可见或不存在的标识不出现在结果中。
    Task<DirectoryResult<IReadOnlyList<DriverStatusSnapshot>>> GetStatusesAsync(
        IReadOnlyList<string> driverIds,
        CancellationToken cancellationToken);
}
