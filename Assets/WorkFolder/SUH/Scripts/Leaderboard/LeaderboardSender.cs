using System.Threading.Tasks;
using DevLib.ModuleSystem;
using DevLib.ServiceLocator;
using UnityEngine;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;
using WorkFolder.SUH.Scripts.Leaderboard.Lib.Interface.ServiceLocator;

namespace WorkFolder.SUH.Scripts.Leaderboard
{
    public class LeaderboardSender : MonoBehaviour, ILeaderboardSender, IAsyncInitModule
    {
        public async Task TryUpdatePlayer(string leaderboardId, uint killCount)
        {
            await AsyncInit();
        }

        public async Task AsyncInit()
        {
            IDataStoreService service = ServiceLocator.Get<IDataStoreService>();

            uint killCount = await service?.GetAsync<uint>(nameof(PlayerDataStore.KillCount))!;
            
        }
    }
}