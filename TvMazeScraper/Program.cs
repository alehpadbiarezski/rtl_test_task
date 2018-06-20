using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using Polly;
using TvMazeScrapper.Core.Abstract;
using TvMazeScrapper.Core.Dal;
using TvMazeScrapper.Core.Logic;

namespace TvMazeScraper
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, false)
                .Build();
            var loggerFactory = new LoggerFactory().AddConsole().AddFile("logs/ts-{Date}.txt"); ;
            var logger = loggerFactory.CreateLogger<Program>();

            var httpClient = new HttpClient();
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.DefaultConnectionLimit = 100;

            var retryPolicy =
                Policy
                    .HandleResult<HttpResponseMessage>(e => e.StatusCode == (System.Net.HttpStatusCode)429)
                    .WaitAndRetryForeverAsync(attempt => TimeSpan.FromSeconds(5));

            var tvMazeClient = new TvMazeClient(httpClient, retryPolicy);
            var storageDbClient = new DocumentClient(new Uri(config["StorageEndpoint"]), config["StorageKey"]);
            logger.LogInformation($"Storage db URI {config["StorageEndpoint"]}");
            var storage = new Storage(config, storageDbClient);
            var scrapper = new Scrapper(tvMazeClient, storage, loggerFactory, int.Parse(config["DegreeOfParallelism"]));
            logger.LogInformation("Initialized all objects. Starting process of grabbing info.");

            MainAsync(scrapper, logger).GetAwaiter().GetResult();
        }

        static async Task MainAsync(IScrapper scrapper, ILogger logger)
        {
            try
            {
                await scrapper.RetreiveDataAsync();
                logger.LogInformation("COMPLETED.");
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex,"Unexpected error.Exit.");
                Console.ReadKey();
            }
        
        }
    }
}
