using LMS.Booking.Application.Abstractions;
using LMS.Booking.Domain.Quotas;

namespace LMS.Booking.Application.Quotas.Commands;

public sealed record UpdatePropertyQuotaCommand(string PropertyId, long ExpectedVersion, PropertyQuotaChanges Changes, CommandContext Context);
