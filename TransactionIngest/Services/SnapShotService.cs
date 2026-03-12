using Microsoft.Extensions.Configuration;
using System.Text.Json;
using TransactionIngest.Models;

namespace TransactionIngest.Services
{
    public class SnapShotService
    {
        //private static readonly HttpClient client = new HttpClient();

        public async Task<string> GetSnapShotTransactions()
        {
            string responseBody = string.Empty;

            // Setup API URL from appsettings using a sample URL
            //  -- NOTE: comment out the mock snapshot data below and also the Program.cs transactionService.Upsert call to view the API returning json
            //try
            //{
            //    responseBody = await client.GetStringAsync(GetSnapShotTransactionApiUrl());

            //    Console.WriteLine("api call json: " + responseBody);
            //}
            //catch (HttpRequestException e)
            //{
            //    Console.WriteLine($"\nException Caught!");
            //    Console.WriteLine($"Message: {e.Message}");
            //}
            //return responseBody;


            // mock snapshot data
            //return Get_Coding_Exercise_Sample_Json();
            //return Get_Coding_Exercise_Sample_Json_With_Duplicate();
            return GetOneTransaction();
            //return GetMultipleTransactions(3);
        }

        private string GetSnapShotTransactionApiUrl()
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            var configuration = builder.Build();
            string apiUrl = configuration["MockedSnapShotTransactionsApi:Url"];

            return apiUrl;
        }

        #region Mocked Data

        private string Get_Coding_Exercise_Sample_Json()
        {
            var transactionList = new List<Transaction>
            {
                new Transaction { Id = 1, CardNumber = "4111111111111111", LocationCode = "STO-01", ProductName = "Wireless Mouse", Amount = 19.99m, TimeStamp = DateTime.UtcNow },
                new Transaction { Id = 2, CardNumber = "4000000000000002", LocationCode = "STO-02", ProductName = "USB-C Cable", Amount = 25.0m, TimeStamp = DateTime.UtcNow },
            };

            return JsonSerializer.Serialize(transactionList, new JsonSerializerOptions { WriteIndented = true });
        }

        private string Get_Coding_Exercise_Sample_Json_With_Duplicate()
        {
            var transactionList = new List<Transaction>
            {
                new Transaction { Id = 1, CardNumber = "4111111111111111", LocationCode = "STO-01", ProductName = "Wireless Mouse", Amount = 19.99m, TimeStamp = DateTime.UtcNow },
                new Transaction { Id = 2, CardNumber = "4000000000000002", LocationCode = "STO-02", ProductName = "USB-C Cable", Amount = 25.0m, TimeStamp = DateTime.UtcNow },
                new Transaction { Id = 1, CardNumber = "4111111111111111", LocationCode = "STO-01", ProductName = "Wireless Mouse v2", Amount = 29.99m, TimeStamp = DateTime.UtcNow },
            };

            return JsonSerializer.Serialize(transactionList, new JsonSerializerOptions { WriteIndented = true });
        }

        private string GetOneTransaction(string cardNumber = "123456789", string locationCode = "STO-03", decimal amount = 9.99m, string productName = "my product")
        {
            var transactionList = new List<Transaction>
            {
                new Transaction { Id = 99, CardNumber = cardNumber, LocationCode = locationCode, Amount = amount, ProductName = productName, TimeStamp = DateTime.UtcNow }
            };

            return JsonSerializer.Serialize(transactionList, new JsonSerializerOptions { WriteIndented = true });
        }

        private string GetMultipleTransactions(int numberTransactions, string cardNumber = "123456789", string locationCode = "STO-4", decimal amount = 9.99m, string productName = "my product")
        {
            var transactionList = new List<Transaction>();

            for (int i = 1; i <= numberTransactions; i++)
            {
                if (i == 3)
                {
                    transactionList.Add(new Transaction { Id = i, CardNumber = cardNumber + i.ToString(), LocationCode = locationCode + i.ToString(), Amount = amount + 1, ProductName = productName + i.ToString(), TimeStamp = DateTime.UtcNow.AddDays(-10) });
                }
                else
                {
                    transactionList.Add(new Transaction { Id = i, CardNumber = cardNumber + i.ToString(), LocationCode = locationCode + i.ToString(), Amount = amount + 1, ProductName = productName + i.ToString(), TimeStamp = DateTime.UtcNow });
                }
                
            };

            return JsonSerializer.Serialize(transactionList, new JsonSerializerOptions {  WriteIndented = true });
        }

        #endregion
    }
}
