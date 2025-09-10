namespace WebApplication1.Services
{
    public interface IJobTestService
    {
        void FireAndForgetJob();
        void ReccuringJob();
        void DelayedJob();
        void ContinuationJob();

        Task SendWelcomeEmailAsync(string email, string name);
        Task ProcessOrderAsync(int orderId);
        Task GenerateReportAsync(string reportType);
    }
}
