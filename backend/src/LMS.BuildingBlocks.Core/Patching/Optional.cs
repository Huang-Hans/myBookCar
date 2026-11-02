namespace LMS.BuildingBlocks.Core.Patching;

// 局部更新字段的三态：未传（IsSet=false）、显式 null（IsSet=true 且 Value 为 null）、新值。
// 普通可空属性无法区分前两种状态。
public readonly record struct Optional<T>(bool IsSet, T? Value)
{
    public static Optional<T> Of(T? value) => new(true, value);

    // 不允许清空的字段：未传或为 null 时保持当前值。
    public T ValueOr(T current) => IsSet && Value is not null ? Value : current;

    // 允许清空的字段：只要传了（包括显式 null）就采用传入值。
    public T? ValueOrCurrent(T? current) => IsSet ? Value : current;
}
