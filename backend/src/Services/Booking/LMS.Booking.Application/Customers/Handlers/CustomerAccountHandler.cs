using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Auditing.Services;
using LMS.Booking.Application.Customers.Commands;
using LMS.Booking.Application.Customers.Models;
using LMS.Booking.Application.Customers.Ports;
using LMS.Booking.Application.Guests.Ports;
using LMS.Booking.Application.Workflows;
using LMS.Booking.Domain.Customers;

namespace LMS.Booking.Application.Customers.Handlers;

// 客户账户资料：快照事实（会员号、等级、积分、登录手机号）只读，本地补充资料（姓名、邮箱）单独保存在 customer_account_profiles，
// 因此宾客快照导入整体替换宾客文档时不会覆盖它。本地姓名只影响账户展示，不改写已有订单里的宾客姓名快照。
internal sealed class CustomerAccountHandler(
    UpsertWorkflow workflow,
    IGuestDirectory guests,
    ICustomerAccountProfileRepository profiles) : ICustomerAccountHandler
{
    private const string Operation = "customer-account.update";
    private const string ResourceType = "customer-account";

    public async Task<OperationResult<CustomerAccount>> GetAsync(string guestId, CancellationToken cancellationToken)
    {
        var guest = await guests.FindAsync(guestId, cancellationToken);
        if (guest is null)
        {
            return OperationResult<CustomerAccount>.Failed(OperationOutcome.NotFound);
        }

        var local = await profiles.FindAsync(guestId, cancellationToken) ?? CustomerAccountProfile.Default(guestId);

        return OperationResult<CustomerAccount>.Succeeded(new CustomerAccount(guest, local));
    }

    public async Task<OperationResult<CustomerAccount>> UpdateAsync(UpdateCustomerAccountCommand command, CancellationToken cancellationToken)
    {
        var guest = await guests.FindAsync(command.GuestId, cancellationToken);
        if (guest is null)
        {
            return OperationResult<CustomerAccount>.Failed(OperationOutcome.NotFound);
        }

        var saved = await workflow.ExecuteAsync(BuildSpecification(command), cancellationToken);

        return saved.IsSuccess
            ? OperationResult<CustomerAccount>.Succeeded(new CustomerAccount(guest, saved.Value!))
            : OperationResult<CustomerAccount>.FailureOf(saved);
    }

    private UpsertSpecification<CustomerAccountProfile> BuildSpecification(UpdateCustomerAccountCommand command) =>
        new(
            command.ExpectedVersion,
            CustomerAccountProfile.Default(command.GuestId),
            (context, token) => profiles.FindAsync(command.GuestId, context, token),
            profile => profile.Version,
            (profile, version) => profile with { Version = version },
            (profile, now) => profile.With(
                command.Name.ValueOr(profile.Name!),
                command.Email.ValueOrCurrent(profile.Email),
                now),
            (before, after) => before.Name == after.Name && before.Email == after.Email,
            (profile, context, token) => profiles.InsertAsync(profile, context, token),
            (profile, expected, context, token) => profiles.TryReplaceAsync(profile, expected, context, token),
            (profile, now) => AuditRecordFactory.Updated(
                command.Context, Operation, ResourceType, profile.GuestId, now, ChangedFields(command), null));

    private static IReadOnlyList<string> ChangedFields(UpdateCustomerAccountCommand command) =>
        new[] { command.Name.IsSet ? "name" : null, command.Email.IsSet ? "email" : null }.OfType<string>().ToList();
}
