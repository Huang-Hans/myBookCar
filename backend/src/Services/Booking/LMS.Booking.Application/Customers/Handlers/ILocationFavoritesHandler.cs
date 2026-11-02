using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Customers.Commands;
using LMS.Booking.Application.Customers.Models;
using LMS.Booking.Domain.Customers;

namespace LMS.Booking.Application.Customers.Handlers;

public interface ILocationFavoritesHandler
{
    // 返回某用途分组的收藏及每项的当前可用性；从未保存过为空分组（版本 0），不写库。
    Task<OperationResult<FavoritesResult>> GetAsync(string guestId, FavoriteUsage usage, CancellationToken cancellationToken);

    Task<OperationResult<FavoritesResult>> ReplaceAsync(ReplaceLocationFavoritesCommand command, CancellationToken cancellationToken);
}
