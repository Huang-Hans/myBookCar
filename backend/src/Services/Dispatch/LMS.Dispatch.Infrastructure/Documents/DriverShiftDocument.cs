using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.Dispatch.Domain.Shifts;
using LMS.Dispatch.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Dispatch.Infrastructure.Documents;

[BsonIgnoreExtraElements]
internal sealed class DriverShiftDocument
{
    [BsonId]
    public string ShiftId { get; init; } = string.Empty;

    // 业务日期以 YYYY-MM-DD 字符串存储，不转换成 UTC 午夜。夜班仍记在开始那天。
    [BsonElement("businessDate")]
    public string BusinessDate { get; init; } = string.Empty;

    [BsonElement("period")]
    public string Period { get; init; } = string.Empty;

    [BsonElement("driverId")]
    public string DriverId { get; init; } = string.Empty;

    [BsonElement("dutyStatus")]
    public string DutyStatus { get; init; } = string.Empty;

    [BsonElement("role")]
    public string Role { get; init; } = string.Empty;

    [BsonElement("attendance")]
    public string Attendance { get; init; } = string.Empty;

    // Retained for document compatibility; uniqueness is enforced by driverId, businessDate and period.
    [BsonElement("driverDateKey")]
    public string DriverDateKey { get; init; } = string.Empty;

    [BsonElement(VersionedUpdate.VersionField)]
    public long Version { get; init; }

    [BsonElement("withdrawnAt")]
    [BsonIgnoreIfNull]
    public DateTime? WithdrawnAt { get; init; }

    [BsonElement("withdrawnBy")]
    [BsonIgnoreIfNull]
    public string? WithdrawnBy { get; init; }

    public static string DriverDateKeyOf(string driverId, DateOnly businessDate) =>
        $"{driverId}|{StoredValues.FromDate(businessDate)}";

    public static DriverShiftDocument FromDomain(DriverShift shift) => new()
    {
        ShiftId = shift.ShiftId,
        BusinessDate = StoredValues.FromDate(shift.BusinessDate),
        Period = WireEnum.ToWire(shift.Period),
        DriverId = shift.DriverId,
        DutyStatus = WireEnum.ToWire(shift.DutyStatus),
        Role = WireEnum.ToWire(shift.Role),
        Attendance = WireEnum.ToWire(shift.Attendance),
        DriverDateKey = DriverDateKeyOf(shift.DriverId, shift.BusinessDate),
        Version = shift.Version,
        WithdrawnAt = shift.WithdrawnAt?.UtcDateTime,
        WithdrawnBy = shift.WithdrawnBy
    };

    public DriverShift ToDomain() => new(
        ShiftId,
        StoredValues.ToDate(BusinessDate, "businessDate"),
        StoredValues.ParseEnum<ShiftPeriod>(Period, "period"),
        DriverId,
        string.IsNullOrEmpty(DutyStatus) ? ShiftDutyStatus.OnDuty : StoredValues.ParseEnum<ShiftDutyStatus>(DutyStatus, "dutyStatus"),
        StoredValues.ParseEnum<ShiftRole>(Role, "role"),
        StoredValues.ParseEnum<ShiftAttendance>(Attendance, "attendance"),
        Version)
    {
        WithdrawnAt = WithdrawnAt is { } instant ? new DateTimeOffset(DateTime.SpecifyKind(instant, DateTimeKind.Utc)) : null,
        WithdrawnBy = WithdrawnBy
    };
}
