namespace LMS.Platform.Application.Abstractions;

// 写入违反唯一性约束。仓储把数据库的重复键错误统一转换为该异常，Application 据此判断并发竞争。
public sealed class DuplicateRecordException(string recordKind, Exception innerException)
    : Exception($"A {recordKind} record with the same unique key already exists.", innerException)
{
    public string RecordKind { get; } = recordKind;
}
