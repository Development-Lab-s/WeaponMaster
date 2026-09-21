using UnityEngine;

namespace WorkFolder.PAP.Scripts.Agent.Part
{
    public abstract class AbstractPartDataSO : ScriptableObject
    {
        public abstract void ChangePart(Transform target ,Vector3 dir,Transform moveTarget ,Vector3 bindPosition , float strength, Transform weaponTrm);
    }
}