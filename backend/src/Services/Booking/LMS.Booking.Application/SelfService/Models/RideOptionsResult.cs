

namespace LMS.Booking.Application.SelfService.Models;

// 车型与路线查询结果：校验通过的路线（含是否跨境）和每个车型产品对该客户的可选结论。只读，不产生任何资格或审批记录。
public sealed record RideOptionsResult(CheckedRoute Route, IReadOnlyList<RideOption> Options);
