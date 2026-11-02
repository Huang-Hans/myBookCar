using LMS.BuildingBlocks.Core.Querying;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Inspections.Models;
using LMS.Dispatch.Domain.Inspections;

namespace LMS.Dispatch.Application.Inspections.Ports;

public interface IDriverInspectionRepository
{
    Task<DriverInspection?> FindByIdAsync(string inspectionId, CancellationToken cancellationToken);

    // 只返回属于该司机的记录；他人的记录与不存在不作区分。
    Task<DriverInspection?> FindForDriverAsync(string inspectionId, string driverId, CancellationToken cancellationToken);

    Task<PageResult<DriverInspection>> ListForDriverAsync(ListInspectionsQuery query, CancellationToken cancellationToken);

    Task InsertAsync(DriverInspection inspection, ITransactionContext context, CancellationToken cancellationToken);
}
