using Core.RedisCache;
using Core.Middleware.RateLimiter;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

string redisConnStr = builder.Configuration.GetConnectionString("Redis") ?? throw new Exception("redis conn str not found");

#region Redis Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnStr;
    options.InstanceName = "RedisUseCaseDemo_";
});
builder.Services.AddScoped<RedisCacheService>();
#endregion

#region Redis client (For IP Rate Limiter)
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnStr));
#endregion


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseIPRateLimiter();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
