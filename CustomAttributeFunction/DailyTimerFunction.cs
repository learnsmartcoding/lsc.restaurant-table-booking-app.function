using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace LSC.CustomAttributeFunction
{
    public static class DailyTimerFunction
    {
        private static readonly HttpClient httpClient = new HttpClient();

        [FunctionName("DailyTimerFunction")]
        public static async Task Run(
            [TimerTrigger("0 0 2 * * *")] TimerInfo myTimer,
            ILogger log)
        {
            if (myTimer.IsPastDue)
            {
                log.LogInformation("The timer is past due!");
            }

            string endpointUrl = "https://lsc-tablebooking.azurewebsites.net/api/updateTimeslots?code=L2s-SHQWtaSgb5M5PB8jCzK4Ce7DGiQpFDByeOmyvzAyAzFu1QMj8w==";

            HttpResponseMessage response = await httpClient.GetAsync(endpointUrl);

            if (response.IsSuccessStatusCode)
            {
                log.LogInformation($"HTTP request successful at {DateTime.UtcNow}");
            }
            else
            {
                log.LogError($"HTTP request failed with status code {response.StatusCode} at {DateTime.UtcNow}");
            }
        }
    }
}
