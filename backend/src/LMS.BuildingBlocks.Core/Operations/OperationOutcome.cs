namespace LMS.BuildingBlocks.Core.Operations;

// 业务用例的结果分类。接入层据此映射 HTTP 状态与应用码，用例本身不依赖 HTTP。
public enum OperationOutcome
{
    // 查询或更新成功。
    Succeeded,

    // 新资源已创建。
    Created,

    // 相同幂等键且请求内容一致，返回此前已提交的资源。
    Replayed,

    // 资源不存在，或对当前调用者的授权范围不可见。
    NotFound,

    // 资源当前状态不允许该操作（例如已决定的审核再次决定）。
    StateConflict,

    // 期望版本与当前版本不一致。
    VersionConflict,

    // 违反唯一性约束（重复创建或改名重复）。
    DuplicateConflict,

    // 相同幂等键但请求内容不同。
    IdempotencyKeyReused,

    // 合并后的资源或请求不满足字段规则，见 Issues。
    ValidationFailed,

    // 请求显式选择了授权范围之外的物业或授权。
    Forbidden,

    // 所需的配置或外部数据源不可用，不能给出真实结果。
    SourceUnavailable,

    // 请求结构合法，但业务前置条件未满足（例如批准前材料未核验），见 Issues。
    BusinessRuleRejected
}
