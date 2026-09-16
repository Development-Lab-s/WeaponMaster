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
        private void Awake()
        {
            DevLib.ServiceLocator.ServiceLocator.Register<ILeaderboardSender>(this);
        }

        private void OnDestroy()
        {
            DevLib.ServiceLocator.ServiceLocator.UnRegister<ILeaderboardSender>(this);
        }

        
        public void AfterInit()
        {


        }

        public async Task TryUpdatePlayer(string leaderboardId, uint killCount)
        {
            ILeaderboardGiver service = ServiceLocator.Get<ILeaderboardGiver>();
        }
    }
}