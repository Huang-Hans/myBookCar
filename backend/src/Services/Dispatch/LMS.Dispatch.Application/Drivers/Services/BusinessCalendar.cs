using LMS.Dispatch.Application.Drivers.Options;
using Microsoft.Extensions.Options;

namespace LMS.Dispatch.Application.Drivers.Services;

internal sealed class BusinessCalendar(IOptions<DispatchBusinessOptions> options, TimeProvider timeProvider)
{
    public DateOnly Today() => DateOnly.FromDateTime(LocalTimeAt(timeProvider.GetUtcNow()));

    public DateTime LocalTimeAt(DateTimeOffset instant)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZoneId);

        return TimeZoneInfo.ConvertTime(instant, zone).DateTime;
    }
}
