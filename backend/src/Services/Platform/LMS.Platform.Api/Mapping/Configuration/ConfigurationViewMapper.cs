using LMS.BuildingBlocks.Core.Wire;
using LMS.Platform.Api.Contracts.Configuration;
using LMS.Platform.Application.Configuration.Models;

namespace LMS.Platform.Api.Mapping.Configuration;

internal static class ConfigurationViewMapper
{
    public static PropertyView ToView(ConfiguredProperty item, IReadOnlyDictionary<string, int>? dailyQuotas) => new(
        item.PropertyId, item.Name, item.Address, item.ContactPerson, item.ContactPhone, item.AssignedFleetCount, item.OperatingHours,
        item.ServiceLevel, item.MinimumLeadTimeHours, item.CancellationPolicyReference, item.MembershipEligibility, DailyQuotaOf(item.PropertyId, dailyQuotas),
        item.VipOverrideRule, item.IsActive, item.IsDeleted, item.Version);

    // dailyQuota 只取自 Booking 的权威限额；Booking 不可用或该物业没有限额记录时省略。
    private static int? DailyQuotaOf(string propertyId, IReadOnlyDictionary<string, int>? dailyQuotas) =>
        dailyQuotas is not null && dailyQuotas.TryGetValue(propertyId, out var quota) ? quota : null;

    public static MembershipTierView ToView(MembershipTier item) => new(
        item.TierCode, item.Name, null, item.MonthlyEntitlementQuota, WireEnum.ToWire(item.BookingPriority), item.VehicleUpgradeReference,
        item.AdvanceBookingWindowHours, item.CancellationFlexibilityReference);

    public static CancellationReasonView ToView(CancellationReason item) => new(
        item.ReasonCode, item.Name, item.PointsReturnPolicyCode, item.PointsReturnReference, item.Source);

    public static LocationCategoryView ToView(LocationCategory item) => new(item.CategoryCode, item.Name, item.Description, item.IsActive);

    public static TravelBufferView ToView(TravelBuffer item) => new(
        item.RouteCategoryCode, item.RouteCategory, item.BufferMinutes, WireEnum.ToWire(item.NavigationProvider),
        item.IsTrafficAwareEtaEnabled, item.ApplicationRule);

    public static NotificationTemplateView ToView(NotificationTemplate item) => new(
        item.TemplateId, WireEnum.ToWire(item.EventType), item.EventTrigger, item.Channels.Select(channel => WireEnum.ToWire(channel)).ToList(),
        item.ContentTemplate, item.IsActive);

    public static NotificationChannelView ToView(NotificationChannelConfiguration item) => new(
        WireEnum.ToWire(item.EventType), item.Channels.Select(channel => WireEnum.ToWire(channel)).ToList(), item.EventTrigger, item.IsActive);

    public static LoyaltyQualificationRulesView ToView(LoyaltyQualificationRules model) => new(
        model.Authority, model.PointsEligibility, model.PlayQualificationReference, model.VisitRequirementReference,
        model.SpecialDesignationReference, model.PointsExpiryReference);

    public static SelfBookingRulesView ToView(SelfBookingRules model) => new(
        model.MinimumLeadTimeHours, model.MaximumAdvanceDays, model.MaximumActiveRequestsPerGuestPerDay, model.PropertyAccessRule,
        model.EligibilitySource);

    public static EligibilityRulesView ToView(EligibilityRules model) => new(
        model.EligibleTierCodes, model.EntitlementRule, model.PointsValidationRule, model.RiskCheckRule, model.ExhaustionRule);

    public static CsQualificationRulesView ToView(CsQualificationRules model) => new(
        model.EligibleGuestRule, model.JustificationRule, model.ApprovalThresholdRule, model.VerificationSource);

    public static RolePermissionView ToView(RolePermissionReference item) => new(
        item.RoleCode, item.RoleName, item.CanManageDrivers, item.CanViewReports, item.CanReviewApprovals, item.CanManageConfiguration);

    public static SystemParametersView ToView(SystemParameters model) => new(
        model.Language, model.TimeZone, model.DateFormat, model.SessionTimeoutMinutes, model.PasswordPolicyReference,
        model.PointsRedemptionMode, model.PointsExpiryReference);

    public static IntegrationReferenceView ToView(IntegrationReference item) => new(
        item.IntegrationCode, item.Name, item.ConfiguredStatus, item.Purpose, item.ConfigurationOwnership, item.EnvironmentLabel,
        item.DataSource, item.AccessPolicy);

    public static ConfigurationRegisterView ToView(ConfigurationRegisterEntry item) => new(
        item.AreaCode, item.AreaName, WireEnum.ToWire(item.Mechanism), item.IsEditable, item.Owner, item.VersionLabel, item.SourceLabel,
        item.ModulePath);
}
