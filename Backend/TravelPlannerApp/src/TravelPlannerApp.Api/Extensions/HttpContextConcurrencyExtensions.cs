using Microsoft.Net.Http.Headers;
using TravelPlannerApp.Application.Common.Utilities;

namespace TravelPlannerApp.Api.Extensions;

public static class HttpContextConcurrencyExtensions
{
    public static string? GetIfMatchVersion(this HttpRequest request)
    {
        return request.Headers[HeaderNames.IfMatch].FirstOrDefault();
    }

    public static void SetETag(this HttpResponse response, string version)
    {
        response.Headers[HeaderNames.ETag] = ConcurrencyTokenHelper.ToETag(version);
    }
}
