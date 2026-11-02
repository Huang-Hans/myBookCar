using System.Text.Json;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Domain.Coupons;

namespace LMS.Dispatch.Application.Coupons.Commands;

public sealed record CreateCouponCommand(
    string Code,
    string Name,
    string MemberId,
    CouponType Type,
    int FaceAmount,
    CouponScope Scope,
    string? PropertyId,
    CouponVehicleRestriction? VehicleRestriction,
    DateOnly ValidFrom,
    DateOnly ValidTo,
    int IssuedQuantity,
    bool Disabled,
    string IdempotencyKey,
    CommandContext Context)
{
    public string ToCanonicalForm() =>
        JsonSerializer.Serialize(new object?[]
        {
            Code.ToUpperInvariant(),
            Name,
            MemberId,
            WireEnum.ToWire(Type),
            FaceAmount,
            WireEnum.ToWire(Scope),
            PropertyId,
            VehicleRestriction is null ? null : WireEnum.ToWire(VehicleRestriction.Value),
            ValidFrom.ToString("yyyy-MM-dd"),
            ValidTo.ToString("yyyy-MM-dd"),
            IssuedQuantity,
            Disabled
        });
}
