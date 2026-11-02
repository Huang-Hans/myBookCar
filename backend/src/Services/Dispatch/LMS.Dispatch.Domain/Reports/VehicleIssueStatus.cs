namespace LMS.Dispatch.Domain.Reports;

// 车辆问题的处理状态。目前只有 RECORDED（已登记）：没有接入运营或维修系统，因此不会出现 FORWARDED / ACKNOWLEDGED，
// 也不会仅凭插入记录就返回已受理或已维修。
public enum VehicleIssueStatus
{
    Recorded,
    Forwarded,
    Acknowledged
}
