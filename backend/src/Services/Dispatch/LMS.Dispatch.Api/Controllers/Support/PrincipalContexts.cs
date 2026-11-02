using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.Http.Tracing;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Dispatch.Application.Abstractions;

namespace LMS.Dispatch.Api.Controllers.Support;

internal static class PrincipalContexts
{
    // 司机本人写操作的调用上下文：操作人是绑定的 driverId，数据范围由 driverId 本身限定而非物业范围。
    public static CommandContext From(HttpContext http) =>
        new(http.GetPrincipal().EntityId, http.GetTraceId(), PropertyScope.AllProperties);
}
