namespace LMS.Platform.Application.Configuration;

// 配置正文在 configuration_assets 中的区域代码。每个区域对应一份强类型正文，与已发布的版本（releaseId）绑定。
public static class ConfigurationAreas
{
    public const string Properties = "PROPERTIES";
    public const string CancellationReasons = "CANCELLATION_REASONS";
    public const string LocationCategories = "LOCATION_CATEGORIES";
    public const string TravelBuffers = "TRAVEL_BUFFERS";
    public const string NotificationTemplates = "NOTIFICATION_TEMPLATES";
    public const string NotificationChannels = "NOTIFICATION_CHANNELS";
    public const string LoyaltyQualificationRules = "LOYALTY_QUALIFICATION_RULES";
    public const string SelfBookingRules = "SELF_BOOKING_RULES";
    public const string EligibilityRules = "SELF_BOOKING_ELIGIBILITY_RULES";
    public const string CsQualificationRules = "CS_QUALIFICATION_RULES";
    public const string RolePermissionReferences = "ROLE_PERMISSION_REFERENCES";
    public const string SystemParameters = "SYSTEM_PARAMETERS";
    public const string IntegrationReferences = "INTEGRATION_REFERENCES";
    public const string ConfigurationRegister = "CONFIGURATION_REGISTER";
    public const string MembershipTiers = "MEMBERSHIP_TIERS";
}
