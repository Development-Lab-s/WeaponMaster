using System.Threading.Tasks;

namespace WorkFolder.SUH.Scripts.DataStoreService.Interface
{
    public interface IReadOnlyDataStore<T>
    {
        Task<T> GetAsync(string key);
    }
}