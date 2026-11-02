using LMS.Platform.Application.Abstractions;

namespace LMS.Platform.Application.Configuration.Commands;

public sealed record DeletePropertyCommand(string PropertyId, long ExpectedVersion, CommandContext Context);
