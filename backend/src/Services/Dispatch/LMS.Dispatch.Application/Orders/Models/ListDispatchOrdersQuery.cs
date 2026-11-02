using LMS.BuildingBlocks.Core.Querying;
using LMS.Dispatch.Application.Abstractions;

namespace LMS.Dispatch.Application.Orders.Models;

public sealed record ListDispatchOrdersQuery(ListQuery List, DispatchOrderFilter Filter, CommandContext Context);
