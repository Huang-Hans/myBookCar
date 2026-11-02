using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Customers.Commands;
using LMS.Booking.Domain.Customers;

namespace LMS.Booking.Application.Customers.Handlers;

public interface ICustomerPreferencesHandler
{
    // 返回真实记录；从未保存过时返回契约默认（版本 0），不写库。
    Task<OperationResult<CustomerPreferences>> GetAsync(string guestId, CancellationToken cancellationToken);

    Task<OperationResult<CustomerPreferences>> UpdateAsync(UpdateCustomerPreferencesCommand command, CancellationToken cancellationToken);
}
