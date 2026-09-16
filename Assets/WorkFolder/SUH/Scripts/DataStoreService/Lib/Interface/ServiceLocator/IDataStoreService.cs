using System.Threading.Tasks;

namespace WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator
{
    public interface IDataStoreService
    {
        Task<TValue> GetAsync<TValue>(string valueName);
        Task<TValue> SetAsync<TValue>(string valueName, TValue value);
        Task<TValue> GetIdAsync<TValue>(string playerId, string valueName);
        Task<string> SetPlayerName<TValue>(string playerName);
        
        
        Task SaveAsync();
    }
}