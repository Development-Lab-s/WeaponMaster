using UnityEngine;

namespace WorkFolder.PAP.Scripts.Agent.Part
{
    public abstract class AbstractPartDataSO : ScriptableObject
    {
        public abstract void ChangePart(Transform target ,Vector3 dir);
    }
}