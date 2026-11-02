using LMS.Booking.Application.SelfService.Options;
using LMS.Booking.Domain.Trips;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Requests;

namespace LMS.Booking.Api.Mapping.Products;

internal static class ProductVehicleMapper
{
    public static VehicleClass Resolve(FieldErrorCollector errors, string? productCode,
        string? vehicleClass, SelfServiceOptions options)
    {
        if (productCode is null) return errors.RequiredEnum<VehicleClass>("vehicleClass", vehicleClass);
        var product = options.EffectiveProducts.FirstOrDefault(item => item.Code == productCode);
        if (product is null)
        {
            errors.Add("productCode", "Unknown service product");
            return default;
        }

        if (!WireEnum.TryParse<VehicleClass>(product.VehicleClass, out var resolved))
            throw new InvalidOperationException($"Unknown vehicle class for product '{product.Code}'.");
        if (vehicleClass is not null && errors.RequiredEnum<VehicleClass>("vehicleClass", vehicleClass) != resolved)
            errors.Add("vehicleClass", "Must match the vehicle class of the selected service product");
        return resolved;
    }
}
