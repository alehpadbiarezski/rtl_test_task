using System.Collections.Generic;
using System.Threading.Tasks;
using TvMazeScrapper.Core.Model;

namespace TvMazeScrapper.Core.Abstract
{
    public interface IStorage
    {
        IList<Show> GetShows(int page, int pageSize);

        Task SaveObjectAsync(Show show);

        Task DeleteAllAsync();
    }
}