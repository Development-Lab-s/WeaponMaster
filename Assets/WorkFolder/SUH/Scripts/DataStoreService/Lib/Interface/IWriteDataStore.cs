using System;
using System.Threading.Tasks;

namespace WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface
{
    public interface IWriteDataStore<T> : IReadOnlyDataStore<T>
    {
        Task SetAsync(string key, T value);
        Task<T> UpdateAsync(string key, Func<T, T> transformFunc);
        Task DeleteAsync(string key);
    }
}