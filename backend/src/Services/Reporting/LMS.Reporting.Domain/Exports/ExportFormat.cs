namespace LMS.Reporting.Domain.Exports;

// 导出文件格式（对外字面值 XLSX / PDF）。PDF 的服务端生成尚未实现，申请时会被明确拒绝。
public enum ExportFormat
{
    Xlsx,
    Pdf
}
