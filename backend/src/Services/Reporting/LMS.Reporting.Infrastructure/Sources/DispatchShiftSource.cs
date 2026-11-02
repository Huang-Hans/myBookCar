using System.Globalization;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Reporting.Application.Refresh.Ports;
using LMS.Reporting.Domain.Sources;
using LMS.Reporting.Infrastructure.ServiceIdentity;
using LMS.Reporting.Infrastructure.Sources.Payloads;

namespace LMS.Reporting.Infrastructure.Sources;

// 读取 Dispatch 的班次。无法识别的业务日期或时段说明来源数据与约定不符，按来源不可用处理而不是悄悄丢弃。
internal sealed class DispatchShiftSource(HttpClient client, IServiceTokenIssuer tokens) : IDispatchShiftSource
{
    private const string SourceName = "dispatch";
    private const string DateFormat = "yyyy-MM-dd";
    private const string OnDutyStatus = "ON_DUTY";

    public async Task<IReadOnlyList<ReportShiftFact>> ListAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var path = $"internal/v1/reporting/shifts?from={Format(from)}&to={Format(to)}";
        var shifts = await SourceHttp.GetAsync<List<DispatchShiftPayload>>(client, tokens, path, SourceName, cancellationToken);

        return shifts.Select(ToFact).ToList();
    }

    private static ReportShiftFact ToFact(DispatchShiftPayload shift)
    {
        var parsedDate = DateOnly.TryParseExact(shift.BusinessDate, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date);
        if (!parsedDate || !WireEnum.TryParse<ReportShiftPeriod>(shift.Period, out var period))
        {
            throw new ReportSourceUnavailableException(SourceName);
        }

        return new ReportShiftFact(shift.DriverId, shift.DriverName, shift.PropertyId, date, period, shift.DutyStatus == OnDutyStatus);
    }

    private static string Format(DateOnly date) => date.ToString(DateFormat, CultureInfo.InvariantCulture);
}
