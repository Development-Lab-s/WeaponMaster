using UnityEngine;
using WorkFolder.PAP.Scripts.Agent.Part;

namespace WorkFolder.PAP.Scripts.SO.DataFolder
{
    [CreateAssetMenu(fileName = "Part offset data", menuName = "SO/Parts/Part offset data", order = 0)]
    public class PartOffsetDataSO : ScriptableObject
    {
        public string targetPartName;
        public Vector3 offset;
        public Vector3 offset2;
        public Vector3 eulerAngles;
        public AbstractPartDataSO bodyPartData;
        public Vector2 driveX;
        
    }
}