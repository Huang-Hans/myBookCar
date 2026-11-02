namespace LMS.Reporting.Domain.Sources;

// 报表关心的行程结局：已完成、已取消，其余状态（待派、进行中等）一律为未结束。
public enum TripOutcome
{
    Open,
    Completed,
    Cancelled
}
