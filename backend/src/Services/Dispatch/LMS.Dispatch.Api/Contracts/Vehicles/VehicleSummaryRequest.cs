using System.ComponentModel;
using LMS.BuildingBlocks.Http.Requests.Contracts;

namespace LMS.Dispatch.Api.Contracts.Vehicles;

public sealed class VehicleSummaryRequest : QueryRequest
{
    [Description("按物业收窄统计；传入时不能为空白。省略表示调用者授权范围内的全部车辆。")]
    public string? PropertyId { get; set; }
}
