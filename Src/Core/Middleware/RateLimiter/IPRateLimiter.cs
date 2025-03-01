using Core.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;

namespace Core.Middleware.RateLimiter;

public class IPRateLimiter
{
    private const string RateLimiterScript = @"
            local current_time = redis.call('TIME')
            local num_windows = ARGV[1]
            for i=2, num_windows*2, 2 do
                local window = ARGV[i]
                local max_requests = ARGV[i+1]
                local curr_key = KEYS[i/2]
                local trim_time = tonumber(current_time[1]) - window
                redis.call('ZREMRANGEBYSCORE', curr_key, 0, trim_time)
                local request_count = redis.call('ZCARD',curr_key)
                if request_count >= tonumber(max_requests) then
                    return 1
                end
            end
            for i=2, num_windows*2, 2 do
                local curr_key = KEYS[i/2]
                local window = ARGV[i]
                redis.call('ZADD', curr_key, current_time[1], current_time[1] .. current_time[2])
                redis.call('EXPIRE', curr_key, window)                
            end
            return 0
            ";
    private readonly IDatabase _db;
    private readonly IConfiguration _config;
    private readonly RequestDelegate _next;

    public IPRateLimiter(RequestDelegate next, IConnectionMultiplexer muxer, IConfiguration config)
    {
        _db = muxer.GetDatabase();
        _config = config;
        _next = next;
    }

    public RateLimitRule[]? GetApplicableRules(HttpContext context)
    {
        var limits = _config.GetSection("RedisRateLimits").Get<RateLimitRule[]>();

        if (limits is null) return null;

        var applicableRules = limits
            .Where(x => x.MatchPath(context.Request.Path))
            .OrderBy(x => x.MaxRequests)
            .GroupBy(x => new { x.PathKey, x.WindowSeconds })
            .Select(x => x.First());

        return applicableRules.ToArray();
    }

    private async Task<bool> IsLimited(RateLimitRule[] rules, string ip)
    {
        var keys = rules.Select(x => new RedisKey($"{x.PathKey}:{{{ip}}}:{x.WindowSeconds}")).ToArray();
        var args = new List<RedisValue> { rules.Length };
        foreach (var rule in rules)
        {
            args.Add(rule.WindowSeconds);
            args.Add(rule.MaxRequests);
        }
        return (int)await _db.ScriptEvaluateAsync(RateLimiterScript, keys, args.ToArray()) == 1;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        // Get IP from HttpContext
        var ip = httpContext.Connection.RemoteIpAddress?.ToString();

        // Get applicable rules
        var applicableRules = GetApplicableRules(httpContext);

        if (ip is not null && applicableRules is not null)
        {
            string ipBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(ip));

            var limited = await IsLimited(applicableRules, ipBase64);

            if (limited)
            {
                httpContext.Response.StatusCode = 429;

                // Set response body json
                httpContext.Response.ContentType = "application/json";

                var response = new ErrorResult
                {
                    ErrorCode = "RateLimitExceeded",
                    ErrorMessage = "Rate limit exceeded"
                };

                // Serialize response to json
                var json = JsonSerializer.Serialize(response);

                // Write json to response body
                await httpContext.Response.WriteAsync(json);

                return;
            }
        }
     
        await _next(httpContext);
    }
}