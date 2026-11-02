using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Coupons.Commands;
using LMS.Dispatch.Application.Coupons.Models;

namespace LMS.Dispatch.Application.Coupons.Handlers;

public interface IUpdateCouponHandler
{
    Task<OperationResult<CouponCard>> HandleAsync(UpdateCouponCommand command, CancellationToken cancellationToken);
}
