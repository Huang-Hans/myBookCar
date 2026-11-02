namespace LMS.Dispatch.Domain.Shifts;

// 班次看板的三段固定班。对外字面值为 MORNING / AFTERNOON / NIGHT。
// 夜班跨过午夜，仍挂在开始那天的业务日期上，不挂在结束那天。
public enum ShiftPeriod
{
    Morning,
    Afternoon,
    Night
}
