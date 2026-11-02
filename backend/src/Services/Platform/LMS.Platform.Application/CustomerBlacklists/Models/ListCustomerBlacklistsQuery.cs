using LMS.BuildingBlocks.Core.Querying;

namespace LMS.Platform.Application.CustomerBlacklists.Models;

public sealed record ListCustomerBlacklistsQuery(ListQuery List, bool? IsActive);
