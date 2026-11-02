using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Auditing.Services;
using LMS.Booking.Application.Guests.Ports;
using LMS.Booking.Application.Qualifications.Commands;
using LMS.Booking.Application.Qualifications.Ports;
using LMS.Booking.Application.Qualifications.Services;
using LMS.Booking.Application.Trips.Services;
using LMS.Booking.Application.Workflows;
using LMS.Booking.Domain.Auditing;
using LMS.Booking.Domain.Guests;
using LMS.Booking.Domain.Qualifications;

namespace LMS.Booking.Application.Qualifications.Handlers;

// 显式发起经理复审申请（原型的 REQUEST MANAGER REVIEW / NEW REQUEST）。
// 资格核对只登记待复核记录，不代表有人提出了申请；这里才是“提出申请”，申请人、时间和理由进入申请快照。
internal sealed class CreateQualificationRequestHandler(
    CreateWorkflow workflow,
    QualificationExceptionExecutor executor,
    QualificationRecorder recorder,
    IGuestDirectory guests,
    IQualificationRepository qualifications,
    TimeProvider timeProvider) : ICreateQualificationRequestHandler
{
    private const string Operation = "qualification.request";
    private const string ResourceType = "qualification-reference";

    public async Task<OperationResult<QualificationReference>> HandleAsync(CreateQualificationRequestCommand command, CancellationToken cancellationToken)
    {
        if (!command.Context.Scope.Allows(command.PropertyId))
        {
            return OperationResult<QualificationReference>.Failed(OperationOutcome.Forbidden);
        }

        var guest = await guests.FindAsync(command.GuestId, cancellationToken);
        if (guest is null)
        {
            return OperationResult<QualificationReference>.Failed(OperationOutcome.NotFound);
        }

        var open = await qualifications.FindOpenRequestByGuestAsync(command.GuestId, command.PropertyId, cancellationToken);

        return open switch
        {
            { Status: QualificationStatus.Escalated } => OperationResult<QualificationReference>.Succeeded(open),
            { } pending => await EscalateAsync(pending, command, cancellationToken),
            _ => await CreateAsync(guest, command, cancellationToken)
        };
    }

    private Task<OperationResult<QualificationReference>> EscalateAsync(
        QualificationReference pending,
        CreateQualificationRequestCommand command,
        CancellationToken cancellationToken) =>
        executor.ExecuteAsync(
            new QualificationExceptionCommand(pending.QualificationId, pending.Version, command.Justification, command.IdempotencyKey, command.Context),
            QualificationTransition.Escalation,
            cancellationToken);

    private Task<OperationResult<QualificationReference>> CreateAsync(
        GuestProfile guest,
        CreateQualificationRequestCommand command,
        CancellationToken cancellationToken) =>
        workflow.ExecuteAsync(
            new CreateSpecification<QualificationReference>(
                IdempotencyScopes.For(command.Context, Operation, command.IdempotencyKey),
                command.ToCanonicalForm(),
                (_, _) => Task.FromResult(recorder.CreateRequested(guest, command.PropertyId, command.Justification, command.Context)),
                reference => reference.QualificationId,
                (reference, context, token) => qualifications.InsertAsync(reference, context, token),
                (id, token) => qualifications.FindByIdAsync(id, token),
                (reference, now) => AuditRecordFactory.Succeeded(
                    command.Context, Operation, ResourceType, reference.QualificationId, now, "Manager review requested", reference.PropertyId,
                    new QualificationAuditContext(guest.GuestId, command.Justification, QualificationStatus.Escalated))),
            cancellationToken);
}
