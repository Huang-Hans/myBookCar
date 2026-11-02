namespace LMS.Dispatch.Application.Reports.Options;

// 物业运营联系资料（OperationsContacts 节，部署配置，Q2-02 的运营联系来源）。没有配置的物业视为不可用，不使用演示号码。
// 列表属性可空：配置绑定对非空集合是追加语义，可空绑定属性整体来自配置。
public sealed class OperationsContactsOptions
{
    public const string SectionName = "OperationsContacts";

    public IReadOnlyList<OperationsContactOption>? Contacts { get; set; }

    public IReadOnlyList<OperationsContactOption> EffectiveContacts => Contacts ?? [];
}

public sealed record OperationsContactOption(string PropertyId, string Name, string Channel, string Value);
