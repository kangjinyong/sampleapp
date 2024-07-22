using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace sampleapp.function
{
    public class TimerTrigger1
    {
        private readonly ILogger<TimerTrigger1> _logger;

        public TimerTrigger1(ILogger<TimerTrigger1> logger)
        {
            _logger = logger;
        }

        [Function("TimerTrigger1")]
        public void Run([TimerTrigger("0 0 22 * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            
            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }
}
