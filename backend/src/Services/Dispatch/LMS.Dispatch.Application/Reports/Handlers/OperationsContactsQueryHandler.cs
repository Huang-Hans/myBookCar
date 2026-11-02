using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Application.Reports.Models;
using LMS.Dispatch.Application.Reports.Options;
using Microsoft.Extensions.Options;

namespace LMS.Dispatch.Application.Reports.Handlers;

// 司机所属物业的运营联系资料：只来自部署配置的可信渠道。没有配置就明确返回不可用，
// 不会回退成司机个人的紧急联系人，也不会返回演示号码；查询资料不表示拨号已接通或运营已受理。
internal sealed class OperationsContactsQueryHandler(IDriverRepository drivers, IOptions<OperationsContactsOptions> options)
    : IOperationsContactsQueryHandler
{
    public async Task<OperationResult<OperationsContacts>> GetAsync(string driverId, CancellationToken cancellationToken)
    {
        var driver = await drivers.FindByIdAsync(driverId, cancellationToken);
        if (driver is null)
        {
            return OperationResult<OperationsContacts>.Failed(OperationOutcome.NotFound);
        }

        var contacts = options.Value.EffectiveContacts
            .Where(contact => string.Equals(contact.PropertyId, driver.PropertyId, StringComparison.Ordinal))
            .Select(contact => new OperationsContact(contact.Name, contact.Channel, contact.Value))
            .ToList();

        return OperationResult<OperationsContacts>.Succeeded(new OperationsContacts(driver.PropertyId, contacts.Count > 0, contacts));
    }
}
