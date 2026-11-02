using LMS.Booking.Application.Console.Options;
using LMS.Booking.Domain.Guests;

namespace LMS.Booking.Application.Previews.Services;

// 等级权益（部署配置 Console:TierEntitlements）的只读查询。
internal static class TierEntitlementLookup
{
    public static IReadOnlyList<string> VehicleClassesOf(ConsoleOptions options, string tierCode) =>
        EntitlementOf(options, tierCode)?.VehicleClasses ?? [];

    // 最低可参与等级：配置的等级权益中，自低到高第一个含可用车型的等级。
    public static string? TierMinimum(ConsoleOptions options) =>
        TierCodes.Ascending
            .Select(code => EntitlementOf(options, code))
            .FirstOrDefault(entitlement => entitlement is { VehicleClasses.Count: > 0 })
            ?.TierCode;

    private static TierEntitlementOption? EntitlementOf(ConsoleOptions options, string tierCode) =>
        options.EffectiveTierEntitlements.FirstOrDefault(tier => string.Equals(tier.TierCode, tierCode, StringComparison.OrdinalIgnoreCase));
}
