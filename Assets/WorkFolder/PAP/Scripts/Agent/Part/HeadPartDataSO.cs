using UnityEngine;

namespace WorkFolder.PAP.Scripts.Agent.Part
{
    [CreateAssetMenu(fileName = "Head Part Data", menuName = "SO/Head Part Data", order = 0)]
    public class HeadPartDataSO : AbstractPartDataSO
    {
        public override void ChangePart(Transform target, Vector3 dir)
        {
            if (dir == Vector3.zero)
            {
                target.localRotation = Quaternion.identity;
                return;
            }
            Vector3 newEuler = Quaternion.FromToRotation(Vector3.up,dir).eulerAngles;
            target.localRotation = Quaternion.Euler(newEuler);
        }
    }
}