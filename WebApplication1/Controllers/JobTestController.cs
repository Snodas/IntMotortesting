using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobTestController : ControllerBase
    {
        private readonly IJobTestService _jobTestService;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ILogger<JobTestController> _logger;

        public JobTestController(IJobTestService jobTestService, IBackgroundJobClient backgroundJobClient, ILogger<JobTestController> logger)
        {
            _jobTestService = jobTestService;
            _backgroundJobClient = backgroundJobClient;
            _logger = logger;
        }

        public class WelcomeEmailRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }

        [HttpGet("/FireAndForgetJob")]
        public ActionResult CreateFireAndForgetJob()
        {
            _backgroundJobClient.Enqueue(() => _jobTestService.FireAndForgetJob());

            return Ok();
        }

        [HttpPost("simple-job")]
        public IActionResult QueueSimpleJob()
        {
            _logger.LogInformation("Queueing simple test job");

            var jobId = BackgroundJob.Enqueue(() => LogSimpleMessage());

            _logger.LogInformation("Simple job {JobId} queued", jobId);

            return Ok(new { JobId = jobId, Message = "Simple job queued" });
        }

        public static void LogSimpleMessage()
        {
            Console.WriteLine($"Simple job executed at {DateTime.Now}");

            Serilog.Log.Information("Simple Hangfire job executed successfully");
        }

        [HttpPost("service-job")]
        public IActionResult QueueServiceJob()
        {
            var jobId = BackgroundJob.Enqueue<WebApplication1.Services.IJobTestService>(
                service => service.SendWelcomeEmailAsync("test@test.com", "Test User"));

            return Ok(new { JobId = jobId });
        }
    }
}
