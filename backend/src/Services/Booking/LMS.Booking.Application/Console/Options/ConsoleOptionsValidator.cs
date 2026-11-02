using LMS.Booking.Domain.Trips;
using LMS.BuildingBlocks.Core.Wire;
using Microsoft.Extensions.Options;

namespace LMS.Booking.Application.Console.Options;

internal sealed class ConsoleOptionsValidator : IValidateOptions<ConsoleOptions>
{
    private const int MaxRetentionDays = 365 * 2;

    public ValidateOptionsResult Validate(string? name, ConsoleOptions options)
    {
        var failures = new List<string>();

        if (options.MinimumLeadTimeMinutes is < 0 or > 24 * 60)
        {
            failures.Add($"{ConsoleOptions.SectionName}:{nameof(ConsoleOptions.MinimumLeadTimeMinutes)} must be between 0 and {24 * 60}.");
        }

        if (options.NotificationRetentionDays is < 0 or > MaxRetentionDays)
        {
            failures.Add($"{ConsoleOptions.SectionName}:{nameof(ConsoleOptions.NotificationRetentionDays)} must be between 0 and {MaxRetentionDays}.");
        }

        failures.AddRange(ValidateVehicleClasses(options));
        failures.AddRange(ValidateTierEntitlements(options));

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }

    private static List<string> ValidateVehicleClasses(ConsoleOptions options)
    {
        var failures = new List<string>();
        var vehicleClasses = options.EffectiveVehicleClasses;
        if (vehicleClasses.Count == 0)
        {
            failures.Add($"{ConsoleOptions.SectionName}:{nameof(ConsoleOptions.VehicleClasses)} must contain at least one vehicle class.");
            return failures;
        }

        foreach (var vehicleClass in vehicleClasses)
        {
            if (!WireEnum.TryParse<VehicleClass>(vehicleClass.Code, out _))
            {
                failures.Add($"{ConsoleOptions.SectionName}:{nameof(ConsoleOptions.VehicleClasses)} contains unknown vehicle class code '{vehicleClass.Code}'.");
            }

            if (vehicleClass.MaxPassengers < 1 || vehicleClass.MaxLuggage < 0)
            {
                failures.Add($"{ConsoleOptions.SectionName}:{nameof(ConsoleOptions.VehicleClasses)} '{vehicleClass.Code}' must allow at least 1 passenger and non-negative luggage.");
            }
        }

        if (vehicleClasses.GroupBy(vehicleClass => vehicleClass.Code).Any(group => group.Count() > 1))
        {
            failures.Add($"{ConsoleOptions.SectionName}:{nameof(ConsoleOptions.VehicleClasses)} contains duplicate codes.");
        }

        return failures;
    }

    private static List<string> ValidateTierEntitlements(ConsoleOptions options)
    {
        var failures = new List<string>();
        var entitlements = options.EffectiveTierEntitlements;
        if (entitlements.Count == 0)
        {
            failures.Add($"{ConsoleOptions.SectionName}:{nameof(ConsoleOptions.TierEntitlements)} must contain at least one tier.");
            return failures;
        }

        var knownCodes = options.EffectiveVehicleClasses.Select(vehicleClass => vehicleClass.Code).ToHashSet(StringComparer.Ordinal);
        foreach (var entitlement in entitlements)
        {
            if (string.IsNullOrWhiteSpace(entitlement.TierCode))
            {
                failures.Add($"{ConsoleOptions.SectionName}:{nameof(ConsoleOptions.TierEntitlements)} contains a blank tier code.");
            }

            if (entitlement.VehicleClasses.Any(code => !knownCodes.Contains(code)))
            {
                failures.Add($"{ConsoleOptions.SectionName}:{nameof(ConsoleOptions.TierEntitlements)} '{entitlement.TierCode}' references unknown vehicle class codes.");
            }
        }

        if (entitlements.GroupBy(entitlement => entitlement.TierCode, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
        {
            failures.Add($"{ConsoleOptions.SectionName}:{nameof(ConsoleOptions.TierEntitlements)} contains duplicate tiers.");
        }

        return failures;
    }
}
