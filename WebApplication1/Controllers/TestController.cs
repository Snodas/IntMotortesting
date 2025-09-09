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

        //[HttpGet("simple-test")]
        //public ActionResult<string> GetSimple()
        //{
        //    return Ok($"Hello {DateTime.Now}");
        //}

        //[HttpGet("cached-test")]
        //public async Task<ActionResult<string>> GetCached()
        //{
        //    var cachedData = await _cache.GetOrSetAsync(
        //        "test-key",
        //        async _ => $"Cached data created at {DateTime.Now}",
        //        TimeSpan.FromMinutes(2)
        //    );

        //    return Ok(cachedData);
        //}

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
                TimeSpan.FromMinutes(5)
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
                TimeSpan.FromMinutes(5),
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
                options => options.SetTags("platina", "documents", "legal")
            );

            await _cache.SetAsync(
                "platina-doc-456",
                new { title = "Document 456", content = "..." },
                TimeSpan.FromHours(1),
                options => options.SetTags("platina", "documents", "hr")
            );

            return Ok("Data cached with tags");
        }

        [HttpDelete("delete-platina")]
        public async Task<ActionResult> RemovePlatina()
        {
            await _cache.RemoveByTagAsync("platina");
            return Ok("All platina data removed!");
        }

    }
}
