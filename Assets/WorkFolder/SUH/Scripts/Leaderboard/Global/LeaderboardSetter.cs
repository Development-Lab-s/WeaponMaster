using System.Threading.Tasks;
using DevLib.ModuleSystem;
using DevLib.ServiceLocator;
using UnityEngine;
using WorkFolder.SUH.Scripts.Leaderboard.Lib.Interface.ServiceLocator;
using WorkFolder.SUH.Scripts.Leaderboard.Local.Lib.Interface.ServiceLocator;

namespace WorkFolder.SUH.Scripts.Leaderboard.Global
{
    public class LeaderboardSetter : MonoBehaviour,
        IAfterInitModule, ILeaderboardSender
    {
        
        public void AfterInit()
        {
            //if (ServiceLocator.Get<ILeaderboardSender>() != null) 

        }

        public async Task TryUpdatePlayer(string leaderboardId, uint killCount)
        {
            ILeaderboardGiver service = ServiceLocator.Get<ILeaderboardGiver>();
        }
    }
}