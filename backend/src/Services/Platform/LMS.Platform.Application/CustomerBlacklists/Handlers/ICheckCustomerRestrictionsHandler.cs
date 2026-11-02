using LMS.Platform.Application.CustomerBlacklists.Models;

namespace LMS.Platform.Application.CustomerBlacklists.Handlers;

public interface ICheckCustomerRestrictionsHandler
{
    // 返回给定会员中当前存在有效限制的那些；会员号不区分大小写，结果使用调用方给出的写法。
    Task<IReadOnlyList<CustomerRestriction>> HandleAsync(IReadOnlyList<string> memberIds, CancellationToken cancellationToken);
}
