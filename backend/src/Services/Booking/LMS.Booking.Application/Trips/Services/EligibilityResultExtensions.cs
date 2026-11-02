using LMS.Booking.Domain.Trips;
using LMS.BuildingBlocks.Core.Operations;

namespace LMS.Booking.Application.Trips.Services;

// 校验链结果到行程结果的形态转换：保留 Outcome 与 Issues，丢弃成功值（调用方另行使用 TripEligibility）。
internal static class EligibilityResultExtensions
{
    public static OperationResult<Trip> ToTripResult(this OperationResult<TripEligibility> source) =>
        new(source.Outcome, default, source.Issues);
}
