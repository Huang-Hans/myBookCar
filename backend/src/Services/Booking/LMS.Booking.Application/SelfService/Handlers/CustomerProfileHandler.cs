using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.SelfService.Models;
using LMS.Booking.Application.SelfService.Services;
using LMS.Booking.Application.SelfService.Ports;

namespace LMS.Booking.Application.SelfService.Handlers;

// 客户本人摘要：预订能力与首页当前旅程使用与订单列表同一份权威行程，选取规则固定为进行中优先、其次最近待出发。
internal sealed class CustomerProfileHandler(GuestBookingGate gate, IGuestTripRepository trips, TimeProvider timeProvider) : ICustomerProfileHandler
{
    public async Task<OperationResult<CustomerSummary>> GetAsync(string guestId, CancellationToken cancellationToken)
    {
        var context = await gate.LoadAsync(guestId, cancellationToken);
        if (!context.IsSuccess)
        {
            return OperationResult<CustomerSummary>.FailureOf(context);
        }

        var current = await trips.FindCurrentForGuestAsync(guestId, timeProvider.GetUtcNow(), cancellationToken);

        return OperationResult<CustomerSummary>.Succeeded(new CustomerSummary(context.Value!.Guest, context.Value.Block, current));
    }
}
