using System.Text.Json;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Domain.Users;

namespace LMS.Platform.Application.Users.Commands;

public sealed record CreatePortalUserCommand(
    string Name,
    string CorporateEmail,
    PortalRole Role,
    bool HasAllPropertyAccess,
    string? PropertyId,
    bool IsActive,
    string IdempotencyKey,
    CommandContext Context)
{
    // 只包含语义输入，不含调用上下文和幂等键本身。
    public string ToCanonicalForm() =>
        JsonSerializer.Serialize(new object?[]
        {
            Name, PortalUser.NormalizeEmail(CorporateEmail), WireEnum.ToWire(Role), HasAllPropertyAccess, PropertyId, IsActive
        });
}
