using Microsoft.AspNetCore.Http;

namespace LMS.BuildingBlocks.Http.Requests.Contracts;

// 查询串请求类的基类：属性即查询参数，由 QueryRequestModelBinder 严格绑定并把汇总的失败留在 BindingFailure。
// 分页列表继承 PagedQueryRequest；不分页的筛选直接继承本类。
public abstract class QueryRequest
{
    internal IResult? BindingFailure { get; set; }
}
