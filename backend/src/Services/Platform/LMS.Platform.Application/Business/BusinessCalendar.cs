using Microsoft.Extensions.Options;

namespace LMS.Platform.Application.Business;

internal sealed class BusinessCalendar(IOptions<PlatformBusinessOptions> options, TimeProvider timeProvider)
{
    public DateOnly Today()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZoneId);

        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), zone).DateTime);
    }
}
