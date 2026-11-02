using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Domain.Shifts;

namespace LMS.Dispatch.Application.Shifts.Ports;

public interface IDriverShiftRepository
{
    Task<bool> SupportsWithdrawalAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<DriverShift>> ListWithdrawalTargetsAsync(IReadOnlyList<string> shiftIds,
        PropertyScope scope, ITransactionContext context, CancellationToken cancellationToken);

    Task<IReadOnlyList<DriverShift>> ListActivePeriodAsync(DateOnly date, ShiftPeriod period, PropertyScope scope,
        string? propertyId, ITransactionContext context, CancellationToken cancellationToken);

    Task<bool> TryWithdrawManyAsync(IReadOnlyList<DriverShift> shifts, string actor, DateTimeOffset now,
        ITransactionContext context, CancellationToken cancellationToken);

    // Global occupancy for the selected period. Property is resolved from current drivers.
    Task<IReadOnlyList<DriverShift>> ListForDriversBetweenAsync(IReadOnlyList<string> driverIds,
        DateOnly from, DateOnly to, ShiftPeriod period, ITransactionContext? context, CancellationToken cancellationToken);

    Task InsertManyAsync(IReadOnlyList<DriverShift> shifts, ITransactionContext context, CancellationToken cancellationToken);

    Task<DriverShift?> FindByIdAsync(string shiftId, CancellationToken cancellationToken);

    // 只返回对调用者物业范围可见的班次；不可见与不存在不作区分。
    Task<DriverShift?> FindVisibleAsync(string shiftId, PropertyScope scope, CancellationToken cancellationToken);

    Task<DriverShift?> FindVisibleAsync(string shiftId, PropertyScope scope, ITransactionContext context, CancellationToken cancellationToken);

    // A driver can have one shift per business date and period, across all properties.
    Task<DriverShift?> FindByDriverInPeriodAsync(string driverId, DateOnly businessDate, ShiftPeriod period, CancellationToken cancellationToken);

    // 一名司机在调用者物业范围内的全部班次。
    Task<PageResult<DriverShift>> ListForDriverAsync(string driverId, PropertyScope scope, ListQuery list, CancellationToken cancellationToken);

    Task<IReadOnlyList<DriverShift>> ListForBoardAsync(
        DateOnly businessDate,
        PropertyScope scope,
        string? propertyId,
        CancellationToken cancellationToken);

    // 闭区间。业务日期以 YYYY-MM-DD 字符串比较，与存储格式一致。
    Task<IReadOnlyList<DriverShift>> ListBetweenAsync(
        DateOnly from,
        DateOnly to,
        PropertyScope scope,
        string? propertyId,
        CancellationToken cancellationToken);

    // Checks current driver bindings in a period; shifts store no vehicle or occupancy key.
    Task<bool> IsVehicleAssignedAsync(
        DateOnly businessDate,
        ShiftPeriod period,
        string vehiclePlate,
        string exceptShiftId,
        ITransactionContext context,
        CancellationToken cancellationToken);

    Task InsertAsync(DriverShift shift, ITransactionContext context, CancellationToken cancellationToken);

    Task<bool> TryReplaceAsync(DriverShift updated, long expectedVersion, ITransactionContext context, CancellationToken cancellationToken);
}
