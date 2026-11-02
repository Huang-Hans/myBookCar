namespace LMS.Dispatch.Api.Contracts.Internal;

/// <summary>
/// 服务间契约：审批前批量核对司机状态的请求体；转发终端用户令牌，并按其物业范围限定结果。
/// </summary>
/// <param name="DriverIds">待核对的司机标识列表；0 至 500 项，每项非空白，服务端去除首尾空白后按序去重。</param>
public sealed record DriverStatusCheckRequest(List<string?>? DriverIds);
