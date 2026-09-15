using UnityEngine;

namespace S2
{
    public readonly struct CritEffectSpawnRequest
    {
        public readonly Vector3 Position;
        public readonly float Size;

        public CritEffectSpawnRequest(Vector3 position, float size)
        {
            Position = position;
            Size = size;
        }
    }
}
