namespace LMS.Dispatch.Domain.Shifts;

// 班次状态。对应新增班次表单：ON_DUTY 当值，REST 休息，LEAVE 请假。
public enum ShiftDutyStatus
{
    OnDuty,
    Rest,
    Leave
}
