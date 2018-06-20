using System.Threading.Tasks;

namespace TvMazeScrapper.Core.Abstract
{
    public interface IScrapper
    {
        Task RetreiveDataAsync();
    }
}