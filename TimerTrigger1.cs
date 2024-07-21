using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace sampleapp.function
{
    public class TimerTrigger1
    {
        [Function("TimerTrigger1")]
        public void Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            
            if (myTimer.ScheduleStatus is not null)
            {
                log.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }
}
