using UnityEngine;

namespace WorkFolder.PAP.Scripts.Agent.Part
{
    public abstract class AbstractPartDataSO : ScriptableObject
    {
        public float x1Axis;
        public float x2Axis;
        public float yAxis;
        public float zAxis;
        public abstract void ChangePart(Transform target ,Vector3 dir);
    }
}