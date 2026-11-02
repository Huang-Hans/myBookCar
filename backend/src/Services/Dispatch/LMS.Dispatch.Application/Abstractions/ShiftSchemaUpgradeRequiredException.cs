namespace LMS.Dispatch.Application.Abstractions;

public sealed class ShiftSchemaUpgradeRequiredException(Exception innerException)
    : Exception("Shift storage still enforces the legacy driver-date constraint.", innerException);
