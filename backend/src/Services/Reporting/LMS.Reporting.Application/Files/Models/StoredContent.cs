namespace LMS.Reporting.Application.Files.Models;

// 内容写入结果。Sha256 是对实际写入字节计算的小写十六进制摘要。
public sealed record StoredContent(StoreOutcome Outcome, long Length, string Sha256);
