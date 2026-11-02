namespace LMS.Reporting.Application.Directory;

public sealed record CallerLookup(CallerLookupOutcome Outcome, PlatformCaller? Caller);
