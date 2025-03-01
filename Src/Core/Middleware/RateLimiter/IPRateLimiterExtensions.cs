using Microsoft.AspNetCore.Builder;

namespace Core.Middleware.RateLimiter;

public static class IPRateLimiterExtensions
{
    public static void UseIPRateLimiter(this IApplicationBuilder builder)
    {
        builder.UseMiddleware<IPRateLimiter>();
    }
}
