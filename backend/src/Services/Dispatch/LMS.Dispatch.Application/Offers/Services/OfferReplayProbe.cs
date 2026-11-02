using LMS.Dispatch.Application.Idempotency.Models;
using LMS.Dispatch.Application.Idempotency.Ports;
using LMS.Dispatch.Application.Idempotency.Services;

namespace LMS.Dispatch.Application.Offers.Services;

// 判断同一幂等键是否已有记录。重试同一个“发送邀请”请求时，邀请仍在占用行程与司机，
// 此时不能按“已有进行中的邀请”拒绝，而应交给创建工作流返回已创建的邀请。
internal sealed class OfferReplayProbe(IIdempotencyRecordRepository records, TimeProvider timeProvider)
{
    public async Task<bool> IsKnownAsync(IdempotencyScope scope, string canonicalForm, CancellationToken cancellationToken)
    {
        var fingerprint = IdempotencyFingerprintFactory.Create(scope, canonicalForm);

        return await records.FindActiveAsync(fingerprint, timeProvider.GetUtcNow(), cancellationToken) is not null;
    }
}
