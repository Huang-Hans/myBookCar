using LMS.Dispatch.Application.Abstractions;

namespace LMS.Dispatch.Application.Orders.Models;

public sealed record DispatchOrderSummaryQuery(string Search, DispatchOrderFilter Filter, CommandContext Context);
