using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Coupons.Commands;
using LMS.Dispatch.Application.Coupons.Models;

namespace LMS.Dispatch.Application.Coupons.Handlers;

public interface ICreateCouponHandler
{
    Task<OperationResult<CouponCard>> HandleAsync(CreateCouponCommand command, CancellationToken cancellationToken);
}
