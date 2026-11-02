using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Application.SelfService.Models;
using LMS.Booking.Application.SelfService.Options;
using LMS.Booking.Application.SelfService.Services;
using Microsoft.Extensions.Options;

namespace LMS.Booking.Application.SelfService.Handlers;

// 服务目录：城市、产品与可订状态。可订由产品配置与客户级前置共同决定，不可订时给出明确原因，而不是隐藏产品或返回示例数据。
internal sealed class CustomerOfferingsHandler(GuestBookingGate gate, IOptions<SelfServiceOptions> options) : ICustomerOfferingsHandler
{
    private static readonly IReadOnlyList<CityOffering> Cities =
    [
        new(ServiceCity.Macau, ["MACAU_PENINSULA", "COTAI", "AIRPORT"]),
        new(ServiceCity.HongKong, ["HONG_KONG"])
    ];

    public async Task<OperationResult<ServiceOfferings>> GetAsync(string guestId, CancellationToken cancellationToken)
    {
        var context = await gate.LoadAsync(guestId, cancellationToken);
        if (!context.IsSuccess)
        {
            return OperationResult<ServiceOfferings>.FailureOf(context);
        }

        var blockReason = context.Value!.Block is { } block ? WireEnum.ToWire(block) : null;
        var products = options.Value.EffectiveProducts.Select(product => OfferingOf(product, blockReason)).ToList();

        return OperationResult<ServiceOfferings>.Succeeded(new ServiceOfferings(Cities, products, context.Value.Rules, options.Value.AsapEnabled));
    }

    private static ProductOffering OfferingOf(ServiceProductOption product, string? blockReason)
    {
        if (!product.IsEnabled)
        {
            return new ProductOffering(product, false, product.UnavailableReason ?? "NOT_CONFIGURED");
        }

        return new ProductOffering(product, blockReason is null, blockReason);
    }
}
