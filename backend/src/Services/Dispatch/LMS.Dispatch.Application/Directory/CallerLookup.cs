namespace LMS.Dispatch.Application.Directory;

public sealed record CallerLookup(CallerLookupOutcome Outcome, PlatformCaller? Caller);
