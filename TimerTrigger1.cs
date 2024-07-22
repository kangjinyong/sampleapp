using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace sampleapp.function
{
    public class TimerTrigger1
    {
        private readonly ILogger _logger;

        public TimerTrigger1(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TimerTrigger1>();
        }

        [Function("TimerTrigger1")]
        public void Run([TimerTrigger("0 0 22 * * *")] TimerInfo myTimer)
        {
            _logger.Log(LogLevel.Information, "Hello World!");
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            
            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }
}
