namespace LMS.ApiGateway.Documentation.Sources;

internal sealed record CachedDocument(string Json, DateTimeOffset FetchedAt);
