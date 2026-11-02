namespace LMS.Reporting.Application.Files.Models;

// 一次已授权的下载：内容流由调用方负责释放。
public sealed record FileDownload(Stream Content, string FileName, string ContentType, long Length);
