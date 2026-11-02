using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Auditing.Services;
using LMS.Booking.Application.Reviews.Commands;
using LMS.Booking.Application.Reviews.Ports;
using LMS.Booking.Application.SelfService.Ports;
using LMS.Booking.Application.Workflows;
using LMS.Booking.Domain.Reviews;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.Reviews.Handlers;

// 评价绑定 tripId + guestId：每次写入都按数据库归属条件核对行程属于本人且已 COMPLETED，不接受请求里的宾客或司机。
// 首次保存创建（版本 1），修改保留创建时间并递增版本；修改已有评价必须带期望版本，相同内容的重复提交返回已保存的评价（结果稳定）。
// 评价不改变行程的版本 / 状态、积分或司机等级；对应的司机取行程指派快照，客户不能任选。
internal sealed class TripReviewHandler(
    UpsertWorkflow workflow,
    IGuestTripRepository trips,
    ITripReviewRepository reviews) : ITripReviewHandler
{
    private const string Operation = "trip-review.upsert";
    private const string ResourceType = "trip-review";

    public async Task<OperationResult<TripReview>> GetAsync(string tripId, string guestId, CancellationToken cancellationToken)
    {
        if (await trips.FindForGuestAsync(tripId, guestId, cancellationToken) is null)
        {
            return OperationResult<TripReview>.Failed(OperationOutcome.NotFound);
        }

        var review = await reviews.FindAsync(tripId, guestId, cancellationToken);

        return review is null ? OperationResult<TripReview>.Failed(OperationOutcome.NotFound) : OperationResult<TripReview>.Succeeded(review);
    }

    public async Task<OperationResult<TripReview>> UpsertAsync(UpsertTripReviewCommand command, CancellationToken cancellationToken)
    {
        var issues = ReviewRules.Validate(command.Rating, command.Comments);
        if (issues.Count > 0)
        {
            return OperationResult<TripReview>.Invalid(issues);
        }

        var trip = await trips.FindForGuestAsync(command.TripId, command.GuestId, cancellationToken);
        if (trip is null)
        {
            return OperationResult<TripReview>.Failed(OperationOutcome.NotFound);
        }

        if (trip.Status != TripStatus.Completed)
        {
            return OperationResult<TripReview>.Rejected([new FieldIssue("tripId", "Only a completed trip can be reviewed")]);
        }

        var existing = await reviews.FindAsync(command.TripId, command.GuestId, cancellationToken);
        if (existing is not null && !existing.HasSameContent(command.Rating, command.Comments) && command.ExpectedVersion == 0)
        {
            return OperationResult<TripReview>.Failed(OperationOutcome.VersionConflict);
        }

        return await workflow.ExecuteAsync(BuildSpecification(command, trip), cancellationToken);
    }

    private UpsertSpecification<TripReview> BuildSpecification(UpsertTripReviewCommand command, Trip trip) =>
        new(
            command.ExpectedVersion,
            new TripReview(trip.TripId, command.GuestId, trip.TripReference, trip.Assignment?.DriverId, 0, null, default, default, 0),
            (context, token) => reviews.FindAsync(command.TripId, command.GuestId, context, token),
            review => review.Version,
            (review, version) => review with { Version = version },
            (review, now) => (review.Version == 0 ? review with { CreatedAt = now } : review).Edit(command.Rating, command.Comments, now),
            (before, after) => before.Version > 0 && before.HasSameContent(after.Rating, after.Comments),
            (review, context, token) => reviews.InsertAsync(review, context, token),
            (review, expected, context, token) => reviews.TryReplaceAsync(review, expected, context, token),
            (review, now) => AuditRecordFactory.Updated(
                command.Context, Operation, ResourceType, review.TripId, now, ["rating", "comments"], trip.PropertyId));
}
