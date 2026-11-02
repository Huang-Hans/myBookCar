using System.Text.Json;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Shifts.Models;

namespace LMS.Dispatch.Application.Shifts.Commands;

public sealed record CreateShiftBatchCommand(
    ShiftBatchRules Rules,
    string PreviewHash,
    string IdempotencyKey,
    CommandContext Context)
{
    public string ToCanonicalForm() => JsonSerializer.Serialize(new[] { Rules.ToCanonicalForm(), PreviewHash });
}
