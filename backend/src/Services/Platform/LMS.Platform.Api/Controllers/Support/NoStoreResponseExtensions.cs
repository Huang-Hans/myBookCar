namespace LMS.Platform.Api.Controllers.Support;

internal static class NoStoreResponseExtensions
{
    private const string NoStore = "no-store";
    private const string NoCache = "no-cache";

    // 凭据与令牌不得被浏览器或中间代理缓存。
    public static void DisableCaching(this HttpResponse response)
    {
        response.Headers.CacheControl = NoStore;
        response.Headers.Pragma = NoCache;
    }
}
