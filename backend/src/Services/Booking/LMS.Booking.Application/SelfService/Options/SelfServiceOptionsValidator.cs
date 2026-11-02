using LMS.Booking.Domain.Trips;
using LMS.BuildingBlocks.Core.Wire;
using Microsoft.Extensions.Options;

namespace LMS.Booking.Application.SelfService.Options;

internal sealed class SelfServiceOptionsValidator : IValidateOptions<SelfServiceOptions>
{
    public ValidateOptionsResult Validate(string? name, SelfServiceOptions options)
    {
        var failures = options.EffectiveProducts
            .Where(product => !WireEnum.TryParse<VehicleClass>(product.VehicleClass, out _))
            .Select(product => $"SelfService:Products '{product.Code}' must map to a known vehicle class.")
            .ToList();
        if (options.EffectiveProducts.GroupBy(product => product.Code).Any(group => group.Count() > 1))
            failures.Add("SelfService:Products contains duplicate product codes.");
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
