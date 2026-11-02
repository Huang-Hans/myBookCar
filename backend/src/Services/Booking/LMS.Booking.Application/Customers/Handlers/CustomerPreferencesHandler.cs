using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Auditing.Services;
using LMS.Booking.Application.Customers.Commands;
using LMS.Booking.Application.Customers.Ports;
using LMS.Booking.Application.Guests.Ports;
using LMS.Booking.Application.Workflows;
using LMS.Booking.Domain.Customers;

namespace LMS.Booking.Application.Customers.Handlers;

// 客户偏好（语言与推送通知总开关）。推送开关只是用户意愿：保存它不代表设备已订阅，也不代表通知已实际投递。
// 偏好不修改会员积分，也不回写已有订单的确认语言。
internal sealed class CustomerPreferencesHandler(
    UpsertWorkflow workflow,
    IGuestDirectory guests,
    ICustomerPreferencesRepository preferences) : ICustomerPreferencesHandler
{
    private const string Operation = "customer-preferences.update";
    private const string ResourceType = "customer-preferences";

    public async Task<OperationResult<CustomerPreferences>> GetAsync(string guestId, CancellationToken cancellationToken)
    {
        if (await guests.FindAsync(guestId, cancellationToken) is null)
        {
            return OperationResult<CustomerPreferences>.Failed(OperationOutcome.NotFound);
        }

        return OperationResult<CustomerPreferences>.Succeeded(
            await preferences.FindAsync(guestId, cancellationToken) ?? CustomerPreferences.Default(guestId));
    }

    public async Task<OperationResult<CustomerPreferences>> UpdateAsync(UpdateCustomerPreferencesCommand command, CancellationToken cancellationToken)
    {
        if (await guests.FindAsync(command.GuestId, cancellationToken) is null)
        {
            return OperationResult<CustomerPreferences>.Failed(OperationOutcome.NotFound);
        }

        return await workflow.ExecuteAsync(BuildSpecification(command), cancellationToken);
    }

    private UpsertSpecification<CustomerPreferences> BuildSpecification(UpdateCustomerPreferencesCommand command) =>
        new(
            command.ExpectedVersion,
            CustomerPreferences.Default(command.GuestId),
            (context, token) => preferences.FindAsync(command.GuestId, context, token),
            record => record.Version,
            (record, version) => record with { Version = version },
            (record, now) => record.With(
                command.Language.ValueOr(record.Language),
                command.IsPushNotificationsEnabled.IsSet ? command.IsPushNotificationsEnabled.Value : record.IsPushNotificationsEnabled,
                now),
            (before, after) => before.Language == after.Language && before.IsPushNotificationsEnabled == after.IsPushNotificationsEnabled,
            (record, context, token) => preferences.InsertAsync(record, context, token),
            (record, expected, context, token) => preferences.TryReplaceAsync(record, expected, context, token),
            (record, now) => AuditRecordFactory.Updated(
                command.Context, Operation, ResourceType, record.GuestId, now, ChangedFields(command), null));

    private static IReadOnlyList<string> ChangedFields(UpdateCustomerPreferencesCommand command) =>
        new[]
        {
            command.Language.IsSet ? "language" : null,
            command.IsPushNotificationsEnabled.IsSet ? "isPushNotificationsEnabled" : null
        }.OfType<string>().ToList();
}
