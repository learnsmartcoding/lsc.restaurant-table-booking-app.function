using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace LSC.RestaurantTableBookingApp.Function
{
    public static class TimeslotGenerationHttpTriggerFunction
    {
        [FunctionName("TimeslotGenerationHttpTriggerFunction")]
        public static async Task<IActionResult> Run(
    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "updateTimeslots")] HttpRequest req,
    ILogger log, ExecutionContext context)
        {
            log.LogInformation($"C# HTTP trigger function executed at: {DateTime.Now}");

            try
            {
                // Get the connection string from the configuration
                var config = new ConfigurationBuilder()
                    .SetBasePath(context.FunctionAppDirectory)
                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();

                string connectionString = config.GetConnectionString("DbContext");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Query to get the RestaurantBranchId and its last ReservationDate
                    string getLastReservationDateQuery = @"
            SELECT DiningTables.RestaurantBranchId, MAX(TimeSlots.ReservationDay) AS LastReservationDate
            FROM DiningTables
            LEFT OUTER JOIN TimeSlots ON DiningTables.Id = TimeSlots.DiningTableId
            GROUP BY DiningTables.RestaurantBranchId";

                    SqlCommand getLastReservationDateCommand = new SqlCommand(getLastReservationDateQuery, connection);

                    List<(int BranchId, DateTime LastReservationDate)> branchData = new List<(int, DateTime)>();

                    using (SqlDataReader reader = await getLastReservationDateCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int branchId = (int)reader["RestaurantBranchId"];
                            DateTime lastReservationDate = (DateTime)reader["LastReservationDate"];
                            branchData.Add((branchId, lastReservationDate));
                        }
                    }

                    // Process each branch
                    foreach (var data in branchData)
                    {
                        int branchId = data.BranchId;
                        DateTime lastReservationDate = data.LastReservationDate;

                        // Calculate the reservation end date (current date + 1 or 2 days)
                        DateTime currentDate = DateTime.Now.Date;
                        DateTime reservationEndDate = currentDate > lastReservationDate ? currentDate.AddDays(2) : lastReservationDate.AddDays(2);

                        if (lastReservationDate <= currentDate.AddDays(2))
                        {


                            // Query to get the DiningTableIds for the branch
                            string getDiningTableIdsQuery = @"
                            SELECT Id AS DiningTableId
                            FROM DiningTables
                            WHERE RestaurantBranchId = @BranchId";

                            SqlCommand getDiningTableIdsCommand = new SqlCommand(getDiningTableIdsQuery, connection);
                            getDiningTableIdsCommand.Parameters.AddWithValue("@BranchId", branchId);

                            List<int> diningTableIds = new List<int>();

                            using (SqlDataReader diningTableIdReader = await getDiningTableIdsCommand.ExecuteReaderAsync())
                            {
                                while (await diningTableIdReader.ReadAsync())
                                {
                                    int diningTableId = (int)diningTableIdReader["DiningTableId"];
                                    diningTableIds.Add(diningTableId);
                                }
                            }

                            // Generate and insert new timeslots for the next 1 or 2 days for each dining table
                            foreach (int diningTableId in diningTableIds)
                            {
                                for (DateTime reservationDate = lastReservationDate.AddDays(1); reservationDate <= reservationEndDate; reservationDate = reservationDate.AddDays(1))
                                {
                                    // Insert available slots into the Timeslots table for each meal type
                                    foreach (string mealType in new string[] { "Breakfast", "Lunch", "Dinner" })
                                    {
                                        string insertTimeslotQuery = @"
                            INSERT INTO TimeSlots (DiningTableId, ReservationDay, MealType, TableStatus)
                            VALUES (@DiningTableId, @ReservationDay, @MealType, @TableStatus)";

                                        SqlCommand insertTimeslotCommand = new SqlCommand(insertTimeslotQuery, connection);
                                        insertTimeslotCommand.Parameters.AddWithValue("@DiningTableId", diningTableId);
                                        insertTimeslotCommand.Parameters.AddWithValue("@ReservationDay", reservationDate);
                                        insertTimeslotCommand.Parameters.AddWithValue("@MealType", mealType);
                                        insertTimeslotCommand.Parameters.AddWithValue("@TableStatus", "Available");

                                        await insertTimeslotCommand.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                        }
                    }
                }

                return new OkObjectResult("Timeslot generation completed successfully.");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred while processing the request.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

    }
}
