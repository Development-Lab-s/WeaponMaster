using System.Collections.Concurrent;
using UnityEngine;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface;

namespace WorkFolder.SUH.Scripts.DataStoreService.Lib
{
    public class DataStoreService : MonoBehaviour
    {
        private static readonly ConcurrentDictionary<string, object> _stores = new ConcurrentDictionary<string, object>();

        public static IDataStore<T> GetDataStore<T>(string storeName) where T : class, new()
        {
            return (IDataStore<T>)_stores.GetOrAdd(storeName, name => new UGSDataStore<T>(name));
        }

        public static IReadOnlyDataStore<T> GetReadOnlyDataStore<T>(string storeName) where T : class, new()
        {
            return GetDataStore<T>(storeName);
        }
    }
}