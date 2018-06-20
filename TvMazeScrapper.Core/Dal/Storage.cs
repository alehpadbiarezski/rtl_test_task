using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;
using TvMazeScrapper.Core.Abstract;
using TvMazeScrapper.Core.Model;

namespace TvMazeScrapper.Core.Dal
{
    public class Storage : IStorage
    {
        private readonly IConfiguration _config;
        private static IDocumentClient _client;

        public Storage(IConfiguration config, IDocumentClient client)
        {
            _config = config;
            _client = client;
        }

        private string DatabaseId => _config["StorageDb"];
        private string CollectionId => _config["StorageCollectionName"];



        public IList<Show> GetShows(int page, int pageSize)
        {
            var topCount = (page + 1) * pageSize;
            var queryTop =
                _client.CreateDocumentQuery<dynamic>(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId),
                        $"SELECT TOP {topCount} c.id FROM c")
                    .AsEnumerable()
                    .Select(json => json.ToString())
                    .Select(str => ((string)str).Substring(7, 36))
                    .Skip(pageSize * page)
                    .Take(pageSize)
                    .ToList();

            var selectIds = string.Join("\",\"", queryTop);
            return
                _client.CreateDocumentQuery<Show>(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId),
                        $"SELECT * FROM c WHERE c.id IN (\"{selectIds}\")")
                    .AsEnumerable()
                    .ToList();
        }

        public async Task SaveObjectAsync(Show show)
        {
            await _client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), show);
        }

        public async Task DeleteAllAsync()
        {
            try
            {
                await _client.DeleteDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                   // nothing to delete, need to create only
                }
                else
                {
                    throw;
                }
            }
            await _client.CreateDocumentCollectionAsync(
                UriFactory.CreateDatabaseUri(DatabaseId),
                new DocumentCollection { Id = CollectionId },
                new RequestOptions { OfferThroughput = 1000 });
        }
    }
}