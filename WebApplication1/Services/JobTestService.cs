
namespace WebApplication1.Services
{
    public class JobTestService : IJobTestService
    {
        private readonly ILogger<JobTestService> _logger;

        public JobTestService(ILogger<JobTestService> logger)
        {
            _logger = logger;
        }

        public void ContinuationJob()
        {
            _logger.LogInformation("Hello from a Continuation job!");
        }

        public void DelayedJob()
        {
            _logger.LogInformation("Hello from a Delayed job!");
        }

        public void FireAndForgetJob()
        {
            _logger.LogInformation("Hello from a Fire and Forget job!");
        }
        public void ReccuringJob()
        {
            _logger.LogInformation("Hello from a Scheduled job!");
        }

        public async Task GenerateReportAsync(string reportType)
        {
            _logger.LogInformation($"Starting to generate {reportType} report", reportType);

            try
            {
                _logger.LogInformation($"Collecting data for {reportType}", reportType);
                await Task.Delay(3000);

                _logger.LogInformation($"Processing data for {reportType}", reportType);
                await Task.Delay(2000);

                _logger.LogInformation($"Formatting {reportType} report", reportType);
                await Task.Delay(1000);

                _logger.LogInformation($"Sucessfully generated {reportType} report", reportType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to generate {reportType} report", reportType);
                throw;
            }
        }

        public async Task ProcessOrderAsync(int orderId)
        {
            _logger.LogInformation($"Starting to process order {orderId}", orderId);

            try
            {
                _logger.LogInformation($"Validating order {orderId}", orderId);
                await Task.Delay(1000);

                _logger.LogInformation($"Processing payment for order {orderId}", orderId);
                await Task.Delay(1500);

                _logger.LogInformation($"Updating inventory for order {orderId}", orderId);
                await Task.Delay(800);

                _logger.LogInformation($"successfully processed order {orderId}", orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process order {orderId}", orderId);
                throw;
            }
        }

        public async Task SendWelcomeEmailAsync(string email, string name)
        {
            _logger.LogInformation($"Starting to send welcome email to {email} for {name}", email, name);

            try
            {
                await Task.Delay(2000);

                if (Random.Shared.Next(1, 6) == 1)
                {
                    throw new Exception("Email service temporarily offline");
                }

                _logger.LogInformation($"Successfully sent welcome email to {email}", email);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send welcome email to {email}", email);
                throw;
            }
        }
    }
}
