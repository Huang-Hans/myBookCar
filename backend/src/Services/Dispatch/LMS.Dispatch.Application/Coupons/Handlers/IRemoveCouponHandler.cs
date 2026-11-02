using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Coupons.Commands;

namespace LMS.Dispatch.Application.Coupons.Handlers;

public interface IRemoveCouponHandler
{
    Task<OperationResult<bool>> HandleAsync(RemoveCouponCommand command, CancellationToken cancellationToken);
}
