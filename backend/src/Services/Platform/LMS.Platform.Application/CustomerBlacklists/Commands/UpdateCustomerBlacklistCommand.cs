using LMS.Platform.Application.Abstractions;
using LMS.Platform.Domain.CustomerBlacklists;

namespace LMS.Platform.Application.CustomerBlacklists.Commands;

public sealed record UpdateCustomerBlacklistCommand(
    string BlacklistId,
    long ExpectedVersion,
    CustomerBlacklistChanges Changes,
    CommandContext Context);
