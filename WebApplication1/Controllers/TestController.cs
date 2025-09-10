using Microsoft.AspNetCore.Mvc;
using System.Reflection.Metadata;
using ZiggyCreatures.Caching.Fusion;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : Controller
    {
        private readonly IFusionCache _cache;

        public TestController(IFusionCache cache)
        {
            _cache = cache;
        }

        // bara FushionCache

        [HttpGet("simple-test")]
        public ActionResult<string> GetSimple()
        {
            return Ok($"Hello {DateTime.Now}");
        }

        [HttpGet("cached-test")]
        public async Task<ActionResult<string>> GetCached()
        {
            var cachedData = await _cache.GetOrSetAsync(
                "test-key",
                async _ => $"Cached data created at {DateTime.Now}",
                TimeSpan.FromMinutes(2)
            );

            return Ok(cachedData);
        }

        // FushionCache mot SQLite

        [HttpPost("store")]
        public async Task<ActionResult<object>> StoreData([FromQuery] string message = "Test Data")
        {
            var data = new
            {
                message = message,
                storedAt = DateTime.Now,
                id = Guid.NewGuid(),
                serverStart = Environment.TickCount64
            };

            await _cache.SetAsync("memory-test", data, TimeSpan.FromMinutes(1));

            return Ok(new
            {
                success = true,
                stored = data,
                note = "Data stored in L1 memory cache only!"
            });
        }

        [HttpGet("retreive")]
        public async Task<ActionResult<object>> RetreiveData()
        {
            var result = await _cache.TryGetAsync<object>("memory-test");

            return Ok(new
            {
                found = result.HasValue,
                data = result.HasValue ? result.Value : null,
                retreivedAt = DateTime.Now,
                currentServerStart = Environment.TickCount64,
                note = result.HasValue ? "Data found in memory cache" : "No cached data found"
            });
        }

        [HttpGet("performance")]
        public async Task<ActionResult<object>> TestPerformance()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var expensiveData = await _cache.GetOrSetAsync(
                "expensive-operation",
                async _ =>
                {
                    await Task.Delay(1000);
                    return new
                    {
                        result = "Expensive data result",
                        computedAt = DateTime.Now
                    };
                },
                TimeSpan.FromMinutes(1)
            );

            stopwatch.Stop();

            return Ok(new
            {
                data = expensiveData,
                executionTimeMs = stopwatch.ElapsedMilliseconds,
                note = "First call takes - 1000ms, subsequent calls should be <10ms"
            });
        }

        [HttpDelete("clear")]
        public async Task<ActionResult<object>> ClearCache()
        {
            await _cache.RemoveAsync("memory-test");
            await _cache.RemoveAsync("expensive- test");

            return Ok(new
            {
                success = true,
                message = "Cache cleared"
            });
        }

        [HttpGet("fail-safe-test")]
        public async Task<ActionResult<object>> FailSafeTest()
        {
            var data = await _cache.GetOrSetAsync<object>(
                "external-service-data",
                async _ =>
                {
                    if (Random.Shared.Next(1, 3) == 1)
                        throw new HttpRequestException("External service is down!");

                    return new {
                        data = "Fresh data from external service",
                        timespamp = DateTime.Now
                    };
                },
                TimeSpan.FromMinutes(1),
                opt => opt.SetFailSafe(true, TimeSpan.FromHours(24))
            );

            return Ok(data);
        }

        [HttpGet("tagged-cache")]
        public async Task<ActionResult<object>> TaggedCacheTest()
        {
            await _cache.SetAsync(
                "platina-doc-123",
                new { title = "Document 123", content = "..." },
                TimeSpan.FromHours(1),
                tags: new[] {"platina", "documents", "legal" }
            );

            await _cache.SetAsync(
                "platina-doc-456",
                new { title = "Document 456", content = "..." },
                TimeSpan.FromHours(1),
                tags: new[] {"platina", "documents", "hr" }
            );

            return Ok("Data cached with tags");
        }

        [HttpDelete("invalidate-platina")]
        public async Task<ActionResult> RemovePlatina()
        {
            await _cache.RemoveByTagAsync("platina");
            return Ok("All platina data invalidated!");
        }

        [HttpGet("conditional-refresh")]
        public async Task<ActionResult<object>> ConditionalRefreshTest()
        {
            var data = await _cache.GetOrSetAsync<object>(
                "auto-refresh-data",
                async _ => new
                {
                    data = "Fresh data",
                    timespamp = DateTime.Now
                },
                TimeSpan.FromMinutes(1),
                options => options
                    .SetEagerRefresh(0.8f)
                    .SetFailSafe(true)
            );

            return Ok(data);
        }

        [HttpGet("with-jitter")]
        public async Task<ActionResult<object>> JitterTest()
        {
            var data = await _cache.GetOrSetAsync<object>(
                "jittered-data",
                async _ => new { timestamp = DateTime.Now },
                TimeSpan.FromMinutes(5),
                options => options.SetJittering(TimeSpan.FromSeconds(30))
            );

            return Ok(data);
        }

        [HttpGet("advanced-patterns")]
        public async Task<ActionResult<object>> AdvancedPatterns()
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var data1 = await _cache.GetOrSetAsync<object>(
                "cancellable-data",
                async (ctx, ct) =>
                {
                    await Task.Delay(1000, ct);
                    return new { result = "Computed with cancellation support" };
                },

                TimeSpan.FromMinutes(5),
                token: cts.Token
            );

            var data2 = await _cache.GetOrSetAsync<object>(
                "context-aware-data",
                async (ctx, ct) =>
                {
                    return new
                    {
                        key = ctx.Key,
                        hasStaleValue = ctx.HasStaleValue,
                        timestamp = DateTime.Now
                    };
                },
                TimeSpan.FromMinutes(5)
            );

            return Ok(new { data1, data2 });
        }

        // Extra tester kan va viktiga... Maybe

        [HttpGet("cache-key-patterns")]
        public async Task<ActionResult<object>> CacheKeyPatterns()
        {
            await _cache.SetAsync("platina:documents:all", "doc data", TimeSpan.FromMinutes(30));
            await _cache.SetAsync("platina:documents:123", "specific data", TimeSpan.FromMinutes(30));
            await _cache.SetAsync("oktav:documents:456", "person data", TimeSpan.FromMinutes(30));
            await _cache.SetAsync("centuri:documents:789", "sample data", TimeSpan.FromMinutes(30));

            return Ok("Cache keys follow naming convention: system:type:id");
        }

        [HttpGet("error-handling")]
        public async Task<ActionResult<object>> ErrorHandlingTest()
        {
            try
            {
                var data = await _cache.GetOrSetAsync<object>(
                    "error-prone-data",
                    async _ =>
                    {
                        if (Random.Shared.Next(1, 5) == 1)
                            throw new HttpRequestException("Network error");

                        if (Random.Shared.Next(1, 5) == 2)
                            throw new HttpRequestException("Service timeout");

                        if (Random.Shared.Next(1, 5) == 3)
                            throw new HttpRequestException("Auth failed");

                        return new { data = "Success!", timestamp = DateTime.Now };
                    },
                    TimeSpan.FromMinutes(5),
                    options => options
                        .SetFailSafe(true, TimeSpan.FromHours(2))
                        .SetFactoryTimeouts(TimeSpan.FromMinutes(30))
                );

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, note = "Fail-safe didnt have stale data" });
            }
        }
    }
}
