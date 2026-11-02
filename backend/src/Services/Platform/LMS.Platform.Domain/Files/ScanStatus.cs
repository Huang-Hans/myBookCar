namespace LMS.Platform.Domain.Files;

// 文件安全状态（技术状态）。对外字面值为 QUARANTINED / CLEAN / INFECTED / FAILED。
// 登记后为 QUARANTINED；摘要或大小与登记不符为 FAILED；扫描器尚未接入，上传核对通过后暂时置为 CLEAN，
// 接入扫描后改回「只有真实的安全检查通过才能是 CLEAN」。
public enum ScanStatus
{
    Quarantined,
    Clean,
    Infected,
    Failed
}
