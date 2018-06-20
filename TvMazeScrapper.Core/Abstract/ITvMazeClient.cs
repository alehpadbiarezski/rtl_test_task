using System.Threading.Tasks;

namespace TvMazeScrapper.Core.Abstract
{
    public interface ITvMazeClient
    {
        Task<string> GetShowsAsync(int page);
        Task<string> GetCastAsync(int showId);
    }
}