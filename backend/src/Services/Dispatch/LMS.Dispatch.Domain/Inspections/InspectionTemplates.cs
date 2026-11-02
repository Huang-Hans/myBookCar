namespace LMS.Dispatch.Domain.Inspections;

// 当前发布的检查模板（来自司机端原型的两组检查项）。模板内容变更必须提高版本号，并保留旧版本以便解释历史记录。
public static class InspectionTemplates
{
    private static readonly InspectionTemplate PreTripV1 = new(
        InspectionPhase.PreTrip,
        1,
        [
            new("TYRES_WHEELS", "Tyres & Wheel Condition"),
            new("LIGHTS_INDICATORS", "Lights & Indicators"),
            new("BRAKES_FLUIDS", "Brakes & Fluid Levels"),
            new("INTERIOR_CLEANLINESS", "Interior Cleanliness"),
            new("SAFETY_EQUIPMENT", "Safety Equipment"),
            new("CROSS_BORDER_DOCUMENTS", "Cross-Border Documents")
        ]);

    private static readonly InspectionTemplate PostTripV1 = new(
        InspectionPhase.PostTrip,
        1,
        [
            new("EXTERIOR_DAMAGE", "Exterior Damage"),
            new("INTERIOR_CONDITION", "Interior Condition"),
            new("FUEL_LEVEL", "Fuel Level"),
            new("LOST_BELONGINGS", "Lost Passenger Belongings"),
            new("LOCK_SECURITY", "Vehicle Lock & Security")
        ]);

    public static IReadOnlyList<InspectionTemplate> Current { get; } = [PreTripV1, PostTripV1];

    public static InspectionTemplate CurrentFor(InspectionPhase phase) =>
        phase == InspectionPhase.PreTrip ? PreTripV1 : PostTripV1;

    // 按阶段与版本取模板；版本不存在（例如客户端缓存了已下线的版本）返回 null。
    public static InspectionTemplate? Find(InspectionPhase phase, int version) =>
        Current.FirstOrDefault(template => template.Phase == phase && template.Version == version);
}
