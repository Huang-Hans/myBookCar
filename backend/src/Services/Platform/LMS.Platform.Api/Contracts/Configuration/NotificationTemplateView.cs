namespace LMS.Platform.Api.Contracts.Configuration;

/// <summary>
/// 通知模板视图，给出模板内容及其适用事件与渠道。
/// </summary>
/// <param name="TemplateId">模板标识。</param>
/// <param name="EventType">事件类型，BOOKING_CONFIRMATION / TRIP_REMINDER / DRIVER_ASSIGNED / TRIP_STARTED / BOOKING_CANCELLED。</param>
/// <param name="EventTrigger">事件触发条件标签。</param>
/// <param name="Channels">使用该模板的渠道，EMAIL / SMS / PUSH / IN_APP；非空且去重。</param>
/// <param name="ContentTemplate">模板内容；变量目录待确认，不接受任意表达式。</param>
/// <param name="IsActive">配置是否启用。</param>
public sealed record NotificationTemplateView(
    string TemplateId,
    string EventType,
    string EventTrigger,
    IReadOnlyList<string> Channels,
    string ContentTemplate,
    bool IsActive);
