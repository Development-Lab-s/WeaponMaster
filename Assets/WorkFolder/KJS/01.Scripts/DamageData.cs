using Fusion;

namespace WorkFolder.KJS._01.Scripts
{
    public struct DamageData : INetworkStruct
    {
        public int Damage;
        public NetworkString<_64> DeathMent;
        public PlayerRef Owner;
        //public PlayerData playerdata;

    }
}