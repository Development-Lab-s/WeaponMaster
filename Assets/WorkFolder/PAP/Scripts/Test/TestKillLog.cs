using DevLib.ServiceLocator;
using UnityEngine;
using WorkFolder.PAP.Scripts.GameSystem.KillSystem;

namespace WorkFolder.PAP.Scripts.Test
{
    public class TestKillLog : MonoBehaviour
    {
        [ContextMenu("ShowKillLog")]
        public void ShowKillLog()
        {
            PlayerData owner = new PlayerData()
            {
                name = "Mr. Tae"
            };
            PlayerData target = new PlayerData()
            {
                name = "Mr. Kim"
            };
            ServiceLocator.Get<IKillLogService>().KillLog(owner, target);
        }
    }
}