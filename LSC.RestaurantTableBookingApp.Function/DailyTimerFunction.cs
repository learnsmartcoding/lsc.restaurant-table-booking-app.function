using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace LSC.RestaurantTableBookingApp.Function
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

            string endpointUrl = "https://restaurant-table-reservation-lsc.azurewebsites.net/api/updateTimeslots?code=HSkxxdvR34wzKOhRx77cSPBmMGrJNByr5KFCtHwo0XHCAzFu5PqWMA==";

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
