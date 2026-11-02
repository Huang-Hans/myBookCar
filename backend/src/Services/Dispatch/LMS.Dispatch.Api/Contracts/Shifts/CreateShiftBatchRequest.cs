using System.ComponentModel;

namespace LMS.Dispatch.Api.Contracts.Shifts;

public sealed class CreateShiftBatchRequest : ShiftBatchRequest
{
    [Description("SHA-256 fingerprint returned by the latest preview of these exact conditions.")]
    public string? PreviewHash { get; init; }
}
