namespace LMS.Dispatch.Domain.Inspections;

// 单项检查结果。FAIL 也是合法结果：不合格项必须能够如实保存，不能因有不合格项而拒绝记录。
public enum InspectionResult
{
    Pass,
    Fail
}
