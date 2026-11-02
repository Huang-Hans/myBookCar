namespace LMS.Booking.Application.SelfService.Ports;

public sealed record CatalogLocationPage(IReadOnlyList<CatalogLocationDetail> Items, int TotalItems);
