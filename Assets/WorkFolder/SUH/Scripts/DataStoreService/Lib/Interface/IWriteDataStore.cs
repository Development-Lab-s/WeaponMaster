using System;
using System.Threading.Tasks;
using UnityEngine;

namespace WorkFolder.SUH.Scripts.DataStoreService.Interface
{
    public interface IWriteDataStore<T> : IReadOnlyDataStore<T>
    {
        Task SetAsync(string key, T value);
        Task UpdateAsync(string key, Func<T, T> transformFunc);
        Task DeleteAsync(string key);
    }
}