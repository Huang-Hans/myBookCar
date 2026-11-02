using LMS.BuildingBlocks.Core.Wire;
using LMS.Reporting.Domain.Rows;
using MongoDB.Bson;

namespace LMS.Reporting.Infrastructure.Repositories;

// 汇总类报表的合并规则：按天存储的行在查询时按“物业”“司机 + 物业”“车辆 + 物业”合并，计数相加；
// 时长与里程只加已知部分，全部未知时为 null，部分未知时标记 isPartial。合并后的文档与 report_rows 同形，可直接按 ReportRowDocument 读取。
internal static class ReportRowMergeStages
{
    public static bool IsMerged(ReportType type) =>
        type is ReportType.DailyJobSummary or ReportType.DriverUtilization or ReportType.VehicleUtilization;

    public static BsonDocument Group(ReportType type) => type switch
    {
        ReportType.DailyJobSummary => GroupDaily(),
        ReportType.DriverUtilization => GroupDriver(),
        ReportType.VehicleUtilization => GroupVehicle(),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "The report type is not merged.")
    };

    public static BsonDocument Project(ReportType type, ServiceType? serviceType) => new()
    {
        ["_id"] = new BsonDocument("$concat", new BsonArray { "merged:", KeyText(type) }),
        ["rowType"] = WireEnum.ToWire(type),
        ["filter"] = new BsonDocument
        {
            ["propertyId"] = "$_id.propertyId",
            ["from"] = "$from",
            ["to"] = "$to",
            ["serviceType"] = serviceType is null ? BsonNull.Value : WireEnum.ToWire(serviceType.Value)
        },
        ["payload"] = PayloadOf(type),
        ["sourceVersion"] = "$sourceVersion",
        ["updatedAt"] = "$updatedAt"
    };

    private static BsonDocument GroupDaily() => WithWindow(new BsonDocument
    {
        ["_id"] = new BsonDocument("propertyId", "$filter.propertyId"),
        ["bookings"] = Sum("$payload.bookings"),
        ["completed"] = Sum("$payload.completed"),
        ["cancellations"] = Sum("$payload.cancellations"),
        ["noShows"] = Sum("$payload.noShows"),
        ["reserved"] = Sum("$payload.redemption.reserved"),
        ["redeemed"] = Sum("$payload.redemption.redeemed"),
        ["returned"] = Sum("$payload.redemption.returned")
    });

    private static BsonDocument GroupDriver() => WithWindow(new BsonDocument
    {
        ["_id"] = new BsonDocument { ["driverId"] = "$payload.driverId", ["propertyId"] = "$filter.propertyId" },
        ["driverName"] = new BsonDocument("$last", "$payload.driverName"),
        ["tripCount"] = Sum("$payload.tripCount"),
        ["idleSeconds"] = Sum("$payload.idleSeconds"),
        ["idleKnown"] = CountNumbers("$payload.idleSeconds"),
        ["overtimeSeconds"] = Sum("$payload.overtimeSeconds"),
        ["overtimeKnown"] = CountNumbers("$payload.overtimeSeconds"),
        ["anyPartial"] = AnyPartial(),
        ["rows"] = Sum(1)
    });

    private static BsonDocument GroupVehicle() => WithWindow(new BsonDocument
    {
        ["_id"] = new BsonDocument { ["vehicleDescription"] = "$payload.vehicleDescription", ["propertyId"] = "$filter.propertyId" },
        ["tripCount"] = Sum("$payload.tripCount"),
        ["mileageMeters"] = Sum("$payload.mileageMeters"),
        ["mileageKnown"] = CountNumbers("$payload.mileageMeters"),
        ["anyPartial"] = AnyPartial(),
        ["rows"] = Sum(1)
    });

    private static BsonDocument PayloadOf(ReportType type) => type switch
    {
        ReportType.DailyJobSummary => new BsonDocument
        {
            ["propertyId"] = "$_id.propertyId",
            ["bookings"] = "$bookings",
            ["completed"] = "$completed",
            ["cancellations"] = "$cancellations",
            ["noShows"] = "$noShows",
            ["redemptionSummary"] = RedemptionSummary()
        },
        ReportType.DriverUtilization => new BsonDocument
        {
            ["driverId"] = "$_id.driverId",
            ["driverName"] = "$driverName",
            ["propertyId"] = "$_id.propertyId",
            ["tripCount"] = "$tripCount",
            ["idleSeconds"] = KnownOrNull("$idleKnown", "$idleSeconds"),
            ["overtimeSeconds"] = KnownOrNull("$overtimeKnown", "$overtimeSeconds"),
            ["redemptionContributionReference"] = BsonNull.Value,
            ["isPartial"] = Partial("$idleKnown", "$overtimeKnown")
        },
        _ => new BsonDocument
        {
            ["vehicleDescription"] = "$_id.vehicleDescription",
            ["mileageMeters"] = KnownOrNull("$mileageKnown", "$mileageMeters"),
            ["tripCount"] = "$tripCount",
            ["downtimeSeconds"] = BsonNull.Value,
            ["redemptionReference"] = BsonNull.Value,
            ["isPartial"] = Partial("$mileageKnown")
        }
    };

    private static BsonValue KeyText(ReportType type) => type switch
    {
        ReportType.DailyJobSummary => "$_id.propertyId",
        ReportType.DriverUtilization => new BsonDocument("$concat", new BsonArray { "$_id.driverId", ":", "$_id.propertyId" }),
        _ => new BsonDocument("$concat", new BsonArray { "$_id.propertyId", ":", "$_id.vehicleDescription" })
    };

    // 合并后的统计窗口取各行窗口的最早起点与最晚终点；版本与更新时间取最新。
    private static BsonDocument WithWindow(BsonDocument group)
    {
        group["from"] = new BsonDocument("$min", "$filter.from");
        group["to"] = new BsonDocument("$max", "$filter.to");
        group["sourceVersion"] = new BsonDocument("$max", "$sourceVersion");
        group["updatedAt"] = new BsonDocument("$max", "$updatedAt");

        return group;
    }

    private static BsonDocument RedemptionSummary()
    {
        var parts = new BsonArray
        {
            "RESERVED ", new BsonDocument("$toString", "$reserved"),
            " · REDEEMED ", new BsonDocument("$toString", "$redeemed"),
            " · RETURNED ", new BsonDocument("$toString", "$returned")
        };
        var empty = new BsonDocument("$eq", new BsonArray { new BsonDocument("$add", new BsonArray { "$reserved", "$redeemed", "$returned" }), 0 });

        return new BsonDocument("$cond", new BsonArray { empty, BsonNull.Value, new BsonDocument("$concat", parts) });
    }

    private static BsonDocument KnownOrNull(string known, string value) =>
        new("$cond", new BsonArray { new BsonDocument("$gt", new BsonArray { known, 0 }), value, BsonNull.Value });

    // 任一行本身是部分数据，或某个字段只在部分行已知。
    private static BsonDocument Partial(params string[] knownCounts)
    {
        var mixed = knownCounts.Select(known => (BsonValue)new BsonDocument("$and", new BsonArray
        {
            new BsonDocument("$gt", new BsonArray { known, 0 }),
            new BsonDocument("$lt", new BsonArray { known, "$rows" })
        }));

        return new BsonDocument("$or", new BsonArray(mixed.Prepend("$anyPartial")));
    }

    private static BsonDocument Sum(BsonValue value) => new("$sum", value);

    private static BsonDocument CountNumbers(string field) =>
        Sum(new BsonDocument("$cond", new BsonArray { new BsonDocument("$isNumber", field), 1, 0 }));

    private static BsonDocument AnyPartial() =>
        new("$max", new BsonDocument("$ifNull", new BsonArray { "$payload.isPartial", false }));
}
