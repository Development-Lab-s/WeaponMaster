using System.Threading.Tasks;

namespace WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface
{
    public interface IReadOnlyDataStore<T>
    {
        Task<T> GetAsync(string key);
    }
}