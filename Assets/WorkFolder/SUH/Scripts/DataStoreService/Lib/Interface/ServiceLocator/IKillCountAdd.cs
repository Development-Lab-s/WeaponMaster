using System.Threading.Tasks;

namespace WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator
{
    public interface IKillCountAdd
    {
        Task KillCountAdd(string playerId, uint killCount);
    }
}