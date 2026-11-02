namespace LMS.Platform.Domain.Exports;

// 导出作业的技术状态（对外字面值 QUEUED / RUNNING / SUCCEEDED / FAILED）。202 只表示受理；
// SUCCEEDED 才表示结果文件已落地。RUNNING 带租约：租约过期的作业可被重新领取。
public enum ExportState
{
    Queued,
    Running,
    Succeeded,
    Failed
}
