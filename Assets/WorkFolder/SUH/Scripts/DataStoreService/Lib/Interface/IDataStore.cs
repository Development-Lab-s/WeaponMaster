using System;
using System.Threading.Tasks;
using WorkFolder.SUH.Scripts.DataStoreService.Interface;

namespace WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface
{
    public interface IDataStore<T> : IReadOnlyDataStore<T>, IWriteDataStore<T> where T : class, new()
    {
        Task<T> IReadOnlyDataStore<T>.GetAsync(string key)
        {
            
        }

        Task IWriteDataStore<T>.SetAsync(string key, T value)
        {
            
        }

        Task IWriteDataStore<T>.UpdateAsync(string key, Func<T, T> transformFunc)
        {
            
        }
    }
}