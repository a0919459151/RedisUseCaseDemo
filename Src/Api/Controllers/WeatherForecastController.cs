using Core.RedisCache;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace RedisCacheDemo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;
        private readonly RedisCacheService _redisCacheService;

        public WeatherForecastController(
            ILogger<WeatherForecastController> logger,
            RedisCacheService redisCacheService)
        {
            _logger = logger;
            _redisCacheService = redisCacheService;
        }

        private static readonly string[] Summaries = [
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        ];


        [HttpGet]
        [Route("Get1")]
        public async Task<IEnumerable<WeatherForecast>> Get1()
        {
            var cacheKey = "WeatherForecast";
            var forecasts = await _redisCacheService.GetAsync<IEnumerable<WeatherForecast>>(cacheKey);

            if (forecasts != null)
            {
                return forecasts;
            }

            forecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5)
            };

            await _redisCacheService.SetAsync(cacheKey, forecasts, cacheOptions);

            return forecasts;
        }

        [HttpGet]
        [Route("Get2")]
        public async Task<IEnumerable<WeatherForecast>> Get2()
        {
            var cacheKey = "WeatherForecast";
            var forecasts = await _redisCacheService.GetAsync<IEnumerable<WeatherForecast>>(cacheKey);

            if (forecasts != null)
            {
                return forecasts;
            }

            forecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5)
            };

            await _redisCacheService.SetAsync(cacheKey, forecasts, cacheOptions);

            return forecasts;
        }
    }
}
