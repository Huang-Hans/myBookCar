namespace LMS.Booking.Api.Contracts.CustomerRecords;

/// <summary>推送投递能力：实时推送尚未接入（第三阶段）前 isPushDeliveryAvailable 恒为 false。</summary>
public sealed record PreferenceDeliveryView(bool IsPushDeliveryAvailable);

/// <summary>
/// 客户偏好。从未保存过时返回契约默认（EN、推送关闭、version=0），不写库。
/// </summary>
/// <param name="Language">界面 / 通知语言：EN / ZH_HANT / ZH_HANS。订单确认语言：请求显式指定优先，其次本人偏好；已有订单不追溯修改。</param>
/// <param name="IsPushNotificationsEnabled">推送通知总开关，只是用户意愿，不代表设备已订阅或通知已投递。</param>
/// <param name="Delivery">实际投递能力。</param>
/// <param name="UpdatedAt">最近保存时间（UTC）；从未保存过时省略。</param>
/// <param name="Version">乐观锁版本号；从未保存过为 0。</param>
public sealed record CustomerPreferencesView(
    string Language,
    bool IsPushNotificationsEnabled,
    PreferenceDeliveryView Delivery,
    DateTimeOffset? UpdatedAt,
    long Version);
