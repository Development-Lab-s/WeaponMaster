using UnityEngine;

namespace WorkFolder.PAP.Scripts.Agent.Part
{
    [CreateAssetMenu(fileName = "Head Part Data", menuName = "SO/Head Part Data", order = 0)]
    public class HeadPartDataSO : AbstractPartDataSO
    {
        public override void ChangePart(Transform target, Vector3 dir,Transform moveTarget,Vector3 bindPosition , float strength)
        {
            moveTarget.localPosition = bindPosition + new Vector3(0, -strength * 1f, 0);
            if (dir == Vector3.zero)
            {
                target.localRotation = Quaternion.identity;
                return;
            }
            Quaternion baseRot = Quaternion.FromToRotation(Vector3.up,dir);
            
            target.localRotation = baseRot; 
        }
    }   
}