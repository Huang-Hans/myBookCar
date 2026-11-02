using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Tracing;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Booking.Application.Abstractions;

namespace LMS.Booking.Api.Controllers.Support;

internal static class PrincipalContexts
{
    // 司机 / 客户本人写操作的调用上下文：操作人是绑定的 driverId / guestId，角色记为主体类型；
    // 数据范围由本人标识限定（仓储查询带 assignment.driverId / guest.guestId），而不是员工的物业范围。
    public static CommandContext From(HttpContext http)
    {
        var principal = http.GetPrincipal();

        return new CommandContext(principal.EntityId, http.GetTraceId(), PropertyScope.AllProperties, WireEnum.ToWire(principal.Type));
    }
}
