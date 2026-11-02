using LMS.Booking.Application.SelfService.Options;
using LMS.Booking.Domain.Trips;
using Microsoft.Extensions.Options;

namespace LMS.Booking.Application.SelfService.Services;

// 客户自助取消的能力判定：先遵守状态机的取消限制（进行中与终态不可取消），再按部署配置的取消窗口（Q06，默认不设）。
// 订单视图的 canCancel、取消政策查询与取消写入使用同一份判定。
public sealed class SelfCancellationRules(IOptions<SelfServiceOptions> options, TimeProvider timeProvider)
{
    public const string StateNotCancellable = "STATE_NOT_CANCELLABLE";
    public const string WithinCancellationCutoff = "WITHIN_CANCELLATION_CUTOFF";

    public bool CanCancel(Trip trip) => BlockedReasonOf(trip) is null;

    // 返回 null 表示可取消；否则给出不可取消的原因代码。
    public string? BlockedReasonOf(Trip trip)
    {
        if (!TripTransitions.CanCancel(trip.Status))
        {
            return StateNotCancellable;
        }

        var cutoff = options.Value.CancellationCutoffMinutes;

        return cutoff > 0 && trip.PickupAt - timeProvider.GetUtcNow() < TimeSpan.FromMinutes(cutoff)
            ? WithinCancellationCutoff
            : null;
    }
}
