using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Core.RedisCache;

public class RedisCacheService
{
    private readonly IDistributedCache _cache;

    public RedisCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        DistributedCacheEntryOptions options,
        CancellationToken token = default)
    {
        var jsonData = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(key, jsonData, options, token);
    }

    public async Task<T?> GetAsync<T>(
        string key,
        CancellationToken token = default)
    {
        var jsonData = await _cache.GetStringAsync(key, token);
        if (jsonData is null) return default;
        return JsonSerializer.Deserialize<T>(jsonData);
    }
}
