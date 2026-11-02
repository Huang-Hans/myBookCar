using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Auditing.Services;
using LMS.Booking.Application.ConsoleNotifications.Services;
using LMS.Booking.Application.Qualifications.Commands;
using LMS.Booking.Application.Qualifications.Ports;
using LMS.Booking.Application.Workflows;
using LMS.Booking.Domain.Auditing;
using LMS.Booking.Domain.Notifications;
using LMS.Booking.Domain.Qualifications;

namespace LMS.Booking.Application.Qualifications.Services;

// 升级、批准与拒绝共用的流程：要求当前状态为 requiredStatus，迁移到 targetStatus，记录真实主体、时间、说明和结构化审计上下文。
// 升级时记下申请快照（谁、何时、为什么）；批准 / 拒绝不覆盖它，并在成功后通知申请人（幂等重放不重发）。
// 不修改外部资格结果、积分或权益参考。
internal sealed class QualificationExceptionExecutor(
    DecisionWorkflow workflow,
    IQualificationRepository qualifications,
    ConsoleNotificationPublisher notifications,
    TimeProvider timeProvider)
{
    private const string ResourceType = "qualification-reference";

    public async Task<OperationResult<QualificationReference>> ExecuteAsync(
        QualificationExceptionCommand command,
        QualificationTransition transition,
        CancellationToken cancellationToken)
    {
        var result = await workflow.ExecuteAsync(BuildSpecification(command, transition), cancellationToken);
        if (result.IsSuccess && result.Outcome != OperationOutcome.Replayed)
        {
            await NotifyRequesterAsync(result.Value!, transition, cancellationToken);
        }

        return result;
    }

    private DecisionSpecification<QualificationReference> BuildSpecification(
        QualificationExceptionCommand command,
        QualificationTransition transition) =>
        new(
            IdempotencyScopes.For(command.Context, transition.Operation, command.IdempotencyKey),
            command.ToCanonicalForm(),
            new PatchSpecification<QualificationReference>(
                command.QualificationId,
                command.ExpectedVersion,
                (id, context, token) => qualifications.FindVisibleAsync(id, command.Context.Scope, context, token),
                qualification => qualification.Version,
                (qualification, _, _) => Task.FromResult(Transition(qualification, command, transition)),
                (updated, expected, context, token) => qualifications.TryReplaceAsync(updated, expected, context, token),
                (_, after, now) => AuditRecordFactory.Succeeded(
                    command.Context, transition.Operation, ResourceType, after.QualificationId, now, transition.Summary, after.PropertyId,
                    new QualificationAuditContext(after.Guest.GuestId, command.Justification, transition.Target))),
            (id, token) => qualifications.FindByIdAsync(id, token));

    private OperationResult<QualificationReference> Transition(
        QualificationReference current,
        QualificationExceptionCommand command,
        QualificationTransition transition)
    {
        if (current.Status != transition.Required)
        {
            return OperationResult<QualificationReference>.Failed(OperationOutcome.StateConflict);
        }

        var now = timeProvider.GetUtcNow();
        var request = transition.Target == QualificationStatus.Escalated
            ? new QualificationRequest(command.Context.Actor, command.Context.ActorName, command.Context.ActorRole, command.Justification, now)
            : current.Request ?? LegacyRequestOf(current);

        return OperationResult<QualificationReference>.Succeeded(
            current.WithOperation(transition.Target, command.Justification, command.Context.Actor, now).WithRequest(request));
    }

    // 存量记录升级时还没有申请快照：此时记录里的操作人 / 时间 / 说明就是升级动作留下的，决定之前把它保存为申请快照。
    private static QualificationRequest LegacyRequestOf(QualificationReference current) =>
        new(current.OperatorId ?? string.Empty, null, null, current.Justification, current.OperatedAt ?? DateTimeOffset.MinValue);

    private async Task NotifyRequesterAsync(QualificationReference after, QualificationTransition transition, CancellationToken cancellationToken)
    {
        if (transition.Target is not (QualificationStatus.Approved or QualificationStatus.Rejected)
            || string.IsNullOrEmpty(after.Request?.RequestedBy))
        {
            return;
        }

        var outcome = transition.Target == QualificationStatus.Approved ? "approved" : "rejected";
        await notifications.PublishStandaloneAsync(
            after.Request.RequestedBy,
            NotificationType.SystemAlert,
            $"Manager review for {after.Guest.Name} was {outcome}.",
            after.TripReference,
            NotificationTarget.ForQualification(after.QualificationId),
            cancellationToken);
    }
}
