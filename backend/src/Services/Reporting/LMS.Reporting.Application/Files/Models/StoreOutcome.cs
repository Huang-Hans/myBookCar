namespace LMS.Reporting.Application.Files.Models;

public enum StoreOutcome
{
    Stored,

    // 内容超过允许的最大字节数，已丢弃。
    Exceeded,

    // 该文件的内容已经存在（并发或重复上传）。
    AlreadyExists
}
