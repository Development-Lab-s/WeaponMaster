using Fusion;
using WorkFolder.PAP.Scripts.Agent.Player;

namespace WorkFolder.KJS._01.Scripts
{
    public struct DamageData : INetworkStruct
    {
        public int Damage;
        public NetworkString<_64> DeathMent;
        public PlayerRef Owner;
        public float ScoreMultiplier;
        public bool IsNone;
    }
}