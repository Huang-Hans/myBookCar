using LMS.Booking.Application.Abstractions;
using LMS.Booking.Domain.Customers;

namespace LMS.Booking.Application.Customers.Commands;

// 整体替换某一用途分组的收藏（空列表清空该分组，另一分组不受影响）。
public sealed record ReplaceLocationFavoritesCommand(
    string GuestId,
    FavoriteUsage Usage,
    IReadOnlyList<string> LocationIds,
    long ExpectedVersion,
    CommandContext Context);
