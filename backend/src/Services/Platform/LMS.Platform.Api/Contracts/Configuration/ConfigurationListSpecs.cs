using LMS.BuildingBlocks.Http.Requests;
using LMS.Platform.Application.Configuration.Definitions;

namespace LMS.Platform.Api.Contracts.Configuration;

// 配置类列表的排序约定取自各自的列表定义，避免请求类与查询引擎各写一份。
internal static class ConfigurationListSpecs
{
    public static ListQuerySpec Of<TItem>(ConfigurationListDefinition<TItem> definition) =>
        new(definition.SortFields.ToList(), definition.DefaultSort, definition.StableSortField);
}
