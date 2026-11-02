using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Booking.Application.Auditing.Ports;
using LMS.Booking.Application.Qualifications.Models;
using LMS.Booking.Application.Qualifications.Ports;
using LMS.Booking.Domain.Auditing;
using LMS.Booking.Domain.Qualifications;

namespace LMS.Booking.Application.Qualifications.Handlers;

internal sealed class QualificationQueryHandler(
    IQualificationRepository qualifications,
    IQualificationAuditReader audits) : IQualificationQueryHandler
{
    public async Task<OperationResult<PageResult<QualificationReference>>> ListAsync(
        ListQualificationsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.PropertyId is not null && !query.Scope.Allows(query.PropertyId))
        {
            return OperationResult<PageResult<QualificationReference>>.Failed(OperationOutcome.Forbidden);
        }

        return OperationResult<PageResult<QualificationReference>>.Succeeded(await qualifications.ListAsync(query, cancellationToken));
    }

    public async Task<OperationResult<QualificationReference>> GetAsync(
        string qualificationId,
        PropertyScope scope,
        CancellationToken cancellationToken)
    {
        var qualification = await qualifications.FindVisibleAsync(qualificationId, scope, cancellationToken);

        return qualification is null
            ? OperationResult<QualificationReference>.Failed(OperationOutcome.NotFound)
            : OperationResult<QualificationReference>.Succeeded(qualification);
    }

    public async Task<OperationResult<PageResult<AuditRecord>>> ListAuditsAsync(
        ListQualificationAuditsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.PropertyId is not null && !query.Scope.Allows(query.PropertyId))
        {
            return OperationResult<PageResult<AuditRecord>>.Failed(OperationOutcome.Forbidden);
        }

        return OperationResult<PageResult<AuditRecord>>.Succeeded(await audits.ListAsync(query, cancellationToken));
    }
}
