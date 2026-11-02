using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.Trips.Models;

public sealed record BookingVehicleModel(VehicleClass VehicleClass, string Model, int Seats);
