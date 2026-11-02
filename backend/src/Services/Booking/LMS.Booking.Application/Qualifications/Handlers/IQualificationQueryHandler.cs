using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Booking.Application.Qualifications.Models;
using LMS.Booking.Domain.Auditing;
using LMS.Booking.Domain.Qualifications;

namespace LMS.Booking.Application.Qualifications.Handlers;

public interface IQualificationQueryHandler
{
    Task<OperationResult<PageResult<QualificationReference>>> ListAsync(ListQualificationsQuery query, CancellationToken cancellationToken);

    Task<OperationResult<QualificationReference>> GetAsync(string qualificationId, PropertyScope scope, CancellationToken cancellationToken);

    Task<OperationResult<PageResult<AuditRecord>>> ListAuditsAsync(ListQualificationAuditsQuery query, CancellationToken cancellationToken);
}
