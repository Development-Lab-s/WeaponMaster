using UnityEngine;

namespace WorkFolder.PAP.Scripts.SO.DataFolder
{
    [CreateAssetMenu(fileName = "Body Part data", menuName = "SO/Data/Body part", order = 0)]
    public class BodyPartDataSO : ScriptableObject
    {
        public string Name;
        public float DamageMultiplier;
    }
}