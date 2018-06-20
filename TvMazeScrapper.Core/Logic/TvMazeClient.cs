using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Polly.Retry;
using TvMazeScrapper.Core.Abstract;

namespace TvMazeScrapper.Core.Logic
{
    public class TvMazeClient : ITvMazeClient
    {
        private HttpClient _httpClient;
        private RetryPolicy<HttpResponseMessage> _retryPolicy;

        public TvMazeClient(HttpClient client, RetryPolicy<HttpResponseMessage> retryPolicy)
        {
            _httpClient = client;
            _retryPolicy = retryPolicy;
        }

        public async Task<string> GetShowsAsync(int page)
        {
            if (page < 0)
            {
                throw new ArgumentException(nameof(page));
            }

            var response = await _retryPolicy.ExecuteAsync(() => _httpClient.GetAsync(GetShowUri(page)));
            return await ParseResponse(response);
        }

        public async Task<string> GetCastAsync(int showId)
        {
            var response = await _retryPolicy.ExecuteAsync(() => _httpClient.GetAsync(GetCastUri(showId)));
            return await ParseResponse(response);
        }

        private static async Task<string> ParseResponse(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                if (HttpStatusCode.NotFound == response.StatusCode)
                {
                    return null;
                }
                else
                {
                    throw new HttpRequestException($"Request failed {response.StatusCode}.");
                }
            }
        }

        private string GetCastUri(int showId)
        {
            return $"http://api.tvmaze.com/shows/{showId}/cast";
        }

        private string GetShowUri(int page)
        {
            return $"http://api.tvmaze.com/shows?page={page}";
        }
    }
}