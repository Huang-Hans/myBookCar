using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.Configuration.Models;

namespace LMS.Platform.Application.Configuration.Commands;

public sealed record UpdatePropertyCommand(string PropertyId, long ExpectedVersion, PropertyChanges Changes, CommandContext Context);
