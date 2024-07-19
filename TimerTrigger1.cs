using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace sampleapp.function
{
    public class TimerTriggerTest
    {
        private readonly ILogger _logger;

        public TimerTriggerTest(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TimerTriggerTest>();
        }

        [Function("TimerTriggerTest")]
        public void Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            
            // if (myTimer.ScheduleStatus is not null)
            // {
            //     _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            // }
        }
    }
}
