namespace LMS.Platform.Application.Configuration.Models;

public sealed record SystemParameters(
    string Language,
    string TimeZone,
    string DateFormat,
    int SessionTimeoutMinutes,
    string PasswordPolicyReference,
    string PointsRedemptionMode,
    string PointsExpiryReference);
