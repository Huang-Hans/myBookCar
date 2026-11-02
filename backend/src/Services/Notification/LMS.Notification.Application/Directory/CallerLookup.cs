namespace LMS.Notification.Application.Directory;

public sealed record CallerLookup(CallerLookupOutcome Outcome, PlatformCaller? Caller);
