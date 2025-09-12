using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Hybrid;
using ZiggyCreatures.Caching.Fusion;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CacheTestController : ControllerBase
    {
        private readonly IFusionCache _cache;
        private readonly ILogger<CacheTestController> _logger;

        public CacheTestController(IFusionCache cache, ILogger<CacheTestController> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        [HttpGet("test")]
        public async Task<ActionResult> TestCache()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var result = await _cache.GetOrSetAsync<object>(
                "test-key",
                async (ctx, ct) =>
                {
                    _logger.LogInformation("Cache Miss - Generating expensive data...");

                    await Task.Delay(2000, ct);

                    return new
                    {
                        Message = "Hello from cache",
                        GeneratedAt = DateTime.Now,
                        Expensivecalculation = Random.Shared.Next(1000, 9999)
                    };
                },
                options => options.SetDuration(TimeSpan.FromMinutes(2)));

            stopwatch.Stop();

            return Ok(new
            {
                Data = result,
                ResponsetimeMs = stopwatch.ElapsedMilliseconds,
                Message = stopwatch.ElapsedMilliseconds > 1000
                    ? "Cache Miss - Data was generated (slow)"
                    : "Cache Hit - Data from cache (fast)"
            });
        }

        [HttpDelete("test-clear")]
        public async Task<ActionResult> ClearTestCache()
        {
            await _cache.RemoveAsync("test-key");
            _logger.LogInformation("Cache cleared");
            return Ok("Cache Cleared - next GET will be slow");
        }

        [HttpGet("info")]
        public ActionResult GetInfo()
        {
            return Ok(new
            {
                Message = "FusionCache is working",
                Instructions = new[]
                {
                    "1. Call GET /api/cachetest/test (should be SLOW - 2 seconds)",
                    "2. Call GET /api/cachetest/test again immediatly (should be FAST - 1ms)",
                    "3. Call DELETE /api/cachetest/test to clear cache",
                    "4. Call GET again (should be SLOW again)",
                }
            });
        }
    }   
}
