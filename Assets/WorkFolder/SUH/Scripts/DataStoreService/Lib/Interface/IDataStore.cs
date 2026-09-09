using System;
using System.Threading.Tasks;

namespace WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface
{
    public interface IDataStore<T> : IReadOnlyDataStore<T>, IWriteDataStore<T> where T : class, new()
    {
        
    }
}