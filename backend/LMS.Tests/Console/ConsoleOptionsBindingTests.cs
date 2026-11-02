using LMS.Booking.Application.Console.Options;
using Microsoft.Extensions.Configuration;

namespace LMS.Tests.Console;

// 锁死配置绑定语义：Console 节提供的列表必须整体生效，不得与内置默认叠加出重复条目
// （该叠加曾导致 booking-api 启动校验失败，见 OptionsValidationException）。
public class ConsoleOptionsBindingTests
{
    [Fact]
    public void BoundVehicleClassesDoNotDuplicateDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Console:MinimumLeadTimeMinutes"] = "15",
                ["Console:VehicleClasses:0:code"] = "LUXURY_SEDAN",
                ["Console:VehicleClasses:0:maxPassengers"] = "3",
                ["Console:VehicleClasses:0:maxLuggage"] = "2",
                ["Console:VehicleClasses:0:pointsRequired"] = "3000",
                ["Console:VehicleClasses:1:code"] = "LUXURY_MPV",
                ["Console:VehicleClasses:1:maxPassengers"] = "6",
                ["Console:VehicleClasses:1:maxLuggage"] = "4",
                ["Console:VehicleClasses:1:pointsRequired"] = "4500",
            })
            .Build();

        var options = new ConsoleOptions();
        configuration.GetSection(ConsoleOptions.SectionName).Bind(options);

        Assert.Equal(2, options.EffectiveVehicleClasses.Count);
        Assert.All(options.EffectiveVehicleClasses, vehicleClass =>
            Assert.Single(options.EffectiveVehicleClasses, candidate => candidate.Code == vehicleClass.Code));
    }

    [Fact]
    public void UnboundOptionsFallBackToDefaults()
    {
        var options = new ConsoleOptions();

        Assert.Equal(3, options.EffectiveVehicleClasses.Count);
        Assert.Equal(5, options.EffectiveTierEntitlements.Count);
    }

    [Fact]
    public void ValidatorAcceptsBoundConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Console:VehicleClasses:0:code"] = "LUXURY_SEDAN",
                ["Console:VehicleClasses:0:maxPassengers"] = "3",
                ["Console:VehicleClasses:0:maxLuggage"] = "2",
                ["Console:VehicleClasses:0:pointsRequired"] = "3000",
                ["Console:TierEntitlements:0:tierCode"] = "DIAMOND",
                ["Console:TierEntitlements:0:vehicleClasses:0"] = "LUXURY_SEDAN",
            })
            .Build();

        var options = new ConsoleOptions();
        configuration.GetSection(ConsoleOptions.SectionName).Bind(options);

        var result = new ConsoleOptionsValidator().Validate(null, options);

        Assert.True(result.Succeeded, string.Join(';', result.Failures ?? []));
    }
}
