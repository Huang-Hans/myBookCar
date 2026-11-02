using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Customers.Commands;
using LMS.Booking.Application.Customers.Models;

namespace LMS.Booking.Application.Customers.Handlers;

public interface ICustomerAccountHandler
{
    // 独立于预订资格：不读取 Platform 规则或客户限制，因此这些来源不可用时账户资料仍可读写。
    Task<OperationResult<CustomerAccount>> GetAsync(string guestId, CancellationToken cancellationToken);

    Task<OperationResult<CustomerAccount>> UpdateAsync(UpdateCustomerAccountCommand command, CancellationToken cancellationToken);
}
