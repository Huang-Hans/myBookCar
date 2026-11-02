using System.Globalization;
using LMS.Reporting.Application.Abstractions;
using LMS.Reporting.Application.Refresh.Models;
using LMS.Reporting.Application.Refresh.Options;
using LMS.Reporting.Application.Refresh.Ports;
using LMS.Reporting.Domain.Building;
using Microsoft.Extensions.Options;

namespace LMS.Reporting.Application.Refresh.Handlers;

// 逐个业务日重算：取得该日的租约后读取行程，纯计算出五类报表行，再在一个事务内整体替换该日的旧行。
// 来源不可用时跳过该日并保留旧行，下一轮重试；其他实例正在重算的业务日直接跳过。
internal sealed class RefreshReportRowsHandler(
    IBookingTripSource trips,
    IDispatchShiftSource shifts,
    ICancellationReasonSource reasons,
    IReportRowWriter writer,
    IReportRefreshLock refreshLock,
    ITransactionRunner transactions,
    IOptions<ReportRefreshOptions> options,
    TimeProvider timeProvider) : IRefreshReportRowsHandler
{
    private readonly string holder = Guid.CreateVersion7().ToString("N");

    public async Task<ReportRefreshResult> RefreshAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var days = Enumerable.Range(0, to.DayNumber - from.DayNumber + 1).Select(from.AddDays).ToList();
        ReportSharedSources shared;
        try
        {
            shared = await LoadSharedAsync(from, to, cancellationToken);
        }
        catch (ReportSourceUnavailableException exception)
        {
            return new ReportRefreshResult(0, days.Count, 0, exception.SourceName);
        }

        var outcomes = new List<ReportDayOutcome>();
        string? unavailableSource = null;
        foreach (var day in days)
        {
            var (outcome, source) = await RefreshDayAsync(day, shared, cancellationToken);
            outcomes.Add(outcome);
            unavailableSource ??= source;
        }

        return new ReportRefreshResult(
            outcomes.Count(outcome => outcome == ReportDayOutcome.Refreshed),
            outcomes.Count(outcome => outcome == ReportDayOutcome.SourceUnavailable),
            outcomes.Count(outcome => outcome == ReportDayOutcome.Locked),
            unavailableSource);
    }

    private async Task<ReportSharedSources> LoadSharedAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var reasonList = await reasons.ListAsync(cancellationToken);
        var shiftList = await shifts.ListAsync(from, to, cancellationToken);

        return new ReportSharedSources(reasonList.ToDictionary(reason => reason.ReasonCode, StringComparer.Ordinal), shiftList);
    }

    private async Task<(ReportDayOutcome Outcome, string? UnavailableSource)> RefreshDayAsync(DateOnly day, ReportSharedSources shared, CancellationToken cancellationToken)
    {
        var key = "day:" + day.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        if (!await refreshLock.TryAcquireAsync(key, holder, TimeSpan.FromMinutes(options.Value.LockLeaseMinutes), cancellationToken))
        {
            return (ReportDayOutcome.Locked, null);
        }

        try
        {
            await ReplaceDayAsync(day, shared, cancellationToken);
            return (ReportDayOutcome.Refreshed, null);
        }
        catch (ReportSourceUnavailableException exception)
        {
            return (ReportDayOutcome.SourceUnavailable, exception.SourceName);
        }
        finally
        {
            await refreshLock.ReleaseAsync(key, holder, CancellationToken.None);
        }
    }

    private async Task ReplaceDayAsync(DateOnly day, ReportSharedSources shared, CancellationToken cancellationToken)
    {
        var dayStart = BusinessDay.StartOf(day);
        var dayTrips = await trips.ListAsync(dayStart, BusinessDay.EndOf(day), cancellationToken);
        var now = timeProvider.GetUtcNow();
        var noShowCodes = options.Value.NoShowReasonCodes.ToHashSet(StringComparer.Ordinal);
        var context = new ReportBuildContext(day, dayTrips, shared.Shifts, shared.Reasons, noShowCodes, now.ToUnixTimeMilliseconds(), now);
        var rows = ReportRowBuilder.Build(context);

        await transactions.ExecuteAsync(
            async (transaction, token) =>
            {
                await writer.ReplaceDayAsync(dayStart, rows, transaction, token);
                return rows.Count;
            },
            cancellationToken);
    }
}
