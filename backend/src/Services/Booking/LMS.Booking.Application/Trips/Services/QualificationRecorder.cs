using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Qualifications.Ports;
using LMS.Booking.Domain.Guests;
using LMS.Booking.Domain.Qualifications;

namespace LMS.Booking.Application.Trips.Services;

// 资格档案登记（HA-02 / HA-09 的联动点）：
// - 资格不满足：登记 MANAGER_APPROVAL_REQUIRED（旁路，失败不改变主响应）；同宾客+物业已有待复核记录时跳过。
// - 建单成功：在同一事务登记 ELIGIBLE 档案并与 Trip 关联（tripReference）。
internal sealed class QualificationRecorder(
    IQualificationRepository qualifications,
    ITransactionRunner transactionRunner,
    TimeProvider timeProvider)
{
    private const string IdPrefix = "qualification_";
    private const string ReferenceCode = "CONSOLE_QUALIFICATION_CHECK";
    private const string EntitlementType = "TRANSFER";
    private static readonly string[] ApprovalChain = ["Operations Manager"];

    public async Task RecordRequiredAsync(
        GuestProfile guest,
        string propertyId,
        string justification,
        CommandContext context,
        string? tripReference,
        CancellationToken cancellationToken)
    {
        try
        {
            var existing = await qualifications.FindPendingByGuestAsync(guest.GuestId, propertyId, cancellationToken);
            if (existing is not null)
            {
                return;
            }

            var reference = Create(guest, propertyId, QualificationStatus.ManagerApprovalRequired, justification, context.Actor, tripReference);
            await transactionRunner.ExecuteAsync(
                (transaction, token) => WrapAsync(qualifications.InsertAsync(reference, transaction, token)),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // 旁路登记失败不改变主响应（422 仍是主结论）；缺失的档案可由再次检查或建单重试补齐。
        }
    }

    public Task RecordEligibleAsync(
        GuestProfile guest,
        string propertyId,
        CommandContext context,
        string tripReference,
        ITransactionContext transaction,
        CancellationToken cancellationToken)
    {
        var reference = Create(guest, propertyId, QualificationStatus.Eligible, "Console booking qualified", context.Actor, tripReference);

        return qualifications.InsertAsync(reference, transaction, cancellationToken);
    }

    // 显式的复审申请：直接进入 ESCALATED（待经理决定），申请快照记下申请人、角色、时间与理由。
    public QualificationReference CreateRequested(GuestProfile guest, string propertyId, string justification, CommandContext context)
    {
        var requested = Create(guest, propertyId, QualificationStatus.Escalated, justification, context.Actor, null);

        return requested.WithRequest(new QualificationRequest(
            context.Actor, context.ActorName, context.ActorRole, justification, requested.OperatedAt ?? timeProvider.GetUtcNow()));
    }

    private QualificationReference Create(
        GuestProfile guest,
        string propertyId,
        QualificationStatus status,
        string justification,
        string operatorId,
        string? tripReference) => new(
            IdPrefix + Guid.CreateVersion7().ToString("N"),
            new GuestSnapshot(guest.GuestId, guest.Name, guest.TierCode),
            propertyId,
            guest.TierCode,
            ReferenceCode,
            EntitlementType,
            guest.PointsBalance,
            null,
            ApprovalChain,
            status,
            justification,
            operatorId,
            timeProvider.GetUtcNow(),
            1)
        { TripReference = tripReference };

    private static async Task<byte> WrapAsync(Task insert) // 事务委托需要返回值；内容为空操作。
    {
        await insert;
        return 0;
    }
}
