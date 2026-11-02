using LMS.BuildingBlocks.Core.Wire;
using LMS.Dispatch.Domain.Shifts;
using LMS.Dispatch.Domain.Shifts.Batches;
using LMS.Dispatch.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Dispatch.Infrastructure.Documents;

[BsonIgnoreExtraElements]
internal sealed class ShiftBatchItemDocument
{
    [BsonElement("businessDate")] public string BusinessDate { get; init; } = string.Empty;
    [BsonElement("driverId")] public string DriverId { get; init; } = string.Empty;
    [BsonElement("driverName")] public string DriverName { get; init; } = string.Empty;
    [BsonElement("isExisting")] public bool IsExisting { get; init; }
    [BsonElement("period")] public string? Period { get; init; }
    [BsonElement("status")] public string? Status { get; init; }
    [BsonElement("role")] public string? Role { get; init; }
    [BsonElement("shiftId")] public string? ShiftId { get; init; }

    public static ShiftBatchItemDocument FromDomain(ShiftBatchItem item) => new()
    {
        BusinessDate = StoredValues.FromDate(item.BusinessDate), DriverId = item.DriverId,
        DriverName = item.DriverName, IsExisting = item.IsExisting,
        Period = item.Period is { } period ? WireEnum.ToWire(period) : null,
        Status = item.DutyStatus is { } status ? WireEnum.ToWire(status) : null,
        Role = item.Role is { } role ? WireEnum.ToWire(role) : null,
        ShiftId = item.ShiftId
    };

    public ShiftBatchItem ToDomain() => new(StoredValues.ToDate(BusinessDate, "businessDate"), DriverId,
        DriverName, IsExisting, Period is null ? null : StoredValues.ParseEnum<ShiftPeriod>(Period, "period"),
        Status is null ? null : StoredValues.ParseEnum<ShiftDutyStatus>(Status, "status"),
        Role is null ? null : StoredValues.ParseEnum<ShiftRole>(Role, "role"), ShiftId, null);
}
