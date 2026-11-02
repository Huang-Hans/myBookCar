using LMS.BuildingBlocks.Http.Requests;
using LMS.Booking.Api.Contracts.Guests;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Guests.Commands;

namespace LMS.Booking.Api.Mapping.Guests;

internal static class GuestRequestMapper
{
    public static RequestParseResult<UpdateGuestCommand> ToCommand(
        string guestId, UpdateGuestRequest request, CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var command = new UpdateGuestCommand(
            guestId,
            errors.PatchText("name", request.Name),
            errors.PatchText("tier", request.Tier),
            request.RoomNumber.PatchNullableText(),
            request.PropertyId.PatchNullableText(),
            context);
        return errors.HasErrors
            ? RequestParseResult<UpdateGuestCommand>.Fail(errors.ToFailure())
            : RequestParseResult<UpdateGuestCommand>.Success(command);
    }
}
