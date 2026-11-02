using LMS.Booking.Domain.Customers;

namespace LMS.Booking.Application.Customers.Models;

public sealed record FavoritesResult(CustomerLocationFavorites Favorites, IReadOnlyList<FavoriteLocationItem> Items);
