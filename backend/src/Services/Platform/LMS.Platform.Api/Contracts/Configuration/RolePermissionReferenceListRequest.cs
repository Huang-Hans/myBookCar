using System.ComponentModel;
using LMS.BuildingBlocks.Http.Requests.Contracts;

namespace LMS.Platform.Api.Contracts.Configuration;

public sealed class RolePermissionReferenceListRequest : QueryRequest
{
    [Description("按物业确认查看范围；传入时不能为空白，超出调用者物业范围返回 403，不改变矩阵内容。")]
    public string? PropertyId { get; set; }
}
