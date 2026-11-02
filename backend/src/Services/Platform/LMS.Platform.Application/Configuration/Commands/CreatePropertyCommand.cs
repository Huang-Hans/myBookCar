using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.Configuration.Models;

namespace LMS.Platform.Application.Configuration.Commands;

public sealed record CreatePropertyCommand(ConfiguredProperty Property, string IdempotencyKey, CommandContext Context);
