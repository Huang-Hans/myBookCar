namespace LMS.ApiGateway.Controllers.Support;

internal static class NoStoreResponseExtensions
{
    private const string NoStore = "no-store";
    private const string NoCache = "no-cache";

    // Credentials and tokens must never be stored by browsers or intermediaries.
    public static void DisableCaching(this HttpResponse response)
    {
        response.Headers.CacheControl = NoStore;
        response.Headers.Pragma = NoCache;
    }
}
