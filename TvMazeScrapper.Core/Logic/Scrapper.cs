using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TvMazeScrapper.Core.Abstract;
using TvMazeScrapper.Core.Model;

namespace TvMazeScrapper.Core.Logic
{
    public class Scrapper : IScrapper
    {
        private readonly ITvMazeClient _tvMazeClient;
        private readonly IStorage _storage;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _semaphoreSlim;
        
        public Scrapper(
            ITvMazeClient tvMazeClient,
            IStorage storage,
            ILoggerFactory loggerFactory,
            int degreeOfParallelism)
        {
            _tvMazeClient = tvMazeClient;
            _storage = storage;
            _logger = loggerFactory.CreateLogger<Scrapper>();
            _semaphoreSlim = new SemaphoreSlim(degreeOfParallelism, degreeOfParallelism);
        }
        public async Task RetreiveDataAsync()
        {
            int pageIndex = 0;
            bool isFault = false;

            _logger.LogInformation("Try to recreate db storage...");
            await _storage.DeleteAllAsync();
            _logger.LogInformation("Done. Start gathering data.");
            
            do
            {
                try
                {
                    var result = await _tvMazeClient.GetShowsAsync(pageIndex++);
                    if (null == result)
                    {
                        _logger.LogInformation("Completed. Nothing to store.");
                        break;
                    }
                    _logger.LogInformation($"Get page {pageIndex}.");

                    var jsonShows = JArray.Parse(result);
                    var tasks = new List<Task>();
                    _logger.LogInformation($"Start saving page {pageIndex} with {jsonShows.Count} shows.");
                    foreach (var show in jsonShows)
                    {
                        tasks.Add(SaveShowAsync(show));
                    }

                    try
                    {
                        await Task.WhenAll(tasks);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Waiting failed.");
                        var exceptions = tasks.Where(t => t.IsFaulted ).Select(t => t.Exception);
                        throw exceptions.First();
                    }
                    
                    _logger.LogInformation($"Done. Saved page {pageIndex} with {jsonShows.Count} shows.");
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "Request failed.");
                    isFault = true;
                }
                catch (JsonException ex)
                {
                    // json invalid, just skip it
                    _logger.LogWarning(ex, "Json operation failed. Skip it.");
                }
            } while (!isFault);
        }

        private async Task SaveShowAsync(JToken jsonShow)
        {
            try
            {
                await _semaphoreSlim.WaitAsync().ConfigureAwait(false);

                var showId = jsonShow["id"].Value<int>();
                var cast = await _tvMazeClient.GetCastAsync(showId);

                if (null == cast)
                {
                    _logger.LogWarning($"Cast for show {showId} is empty. Skip it.");
                    return;
                }

                var jsonCast = JArray.Parse(cast);
                var actors = jsonCast.Select(a => new Actor()
                {
                    RawId = a["person"]["id"].Value<int>(),
                    Name = a["person"]["name"].ToString(),
                    Birthday = a["person"]["birthday"].ToString()
                }).ToArray();

                var newShowObj = new Show()
                {
                    RawId = showId,
                    Name = jsonShow["name"].ToString(),
                    Cast = actors
                };

                await _storage.SaveObjectAsync(newShowObj);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Request failed.");
            }
            catch (JsonException ex)
            {
                // json invalid, just skip it
                _logger.LogWarning(ex, "Json operation failed. Skip it.");
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
    }
}