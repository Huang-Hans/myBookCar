using LMS.Reporting.Application.Refresh.Ports;
using LMS.Reporting.Domain.Sources;
using LMS.Reporting.Infrastructure.ServiceIdentity;
using LMS.Reporting.Infrastructure.Sources.Payloads;

namespace LMS.Reporting.Infrastructure.Sources;

internal sealed class PlatformCancellationReasonSource(HttpClient client, IServiceTokenIssuer tokens) : ICancellationReasonSource
{
    private const string SourceName = "platform";
    private const string Path = "internal/v1/reporting/cancellation-reasons";

    public async Task<IReadOnlyList<CancellationReasonInfo>> ListAsync(CancellationToken cancellationToken)
    {
        var reasons = await SourceHttp.GetAsync<List<PlatformCancellationReasonPayload>>(client, tokens, Path, SourceName, cancellationToken);

        return reasons
            .Select(reason => new CancellationReasonInfo(reason.ReasonCode, reason.Name, reason.PointsReturnPolicyCode, reason.PointsReturnReference))
            .ToList();
    }
}
