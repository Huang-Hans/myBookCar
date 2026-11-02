namespace LMS.BuildingBlocks.Http.Requests.Contracts;

// 标记查询请求类所属的动作本身接收正文（例如上传内容的 PUT）：绑定时不再因携带正文而判为请求结构错误。
[AttributeUsage(AttributeTargets.Class)]
public sealed class AllowRequestBodyAttribute : Attribute;
