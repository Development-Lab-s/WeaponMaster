using UnityEngine;

namespace WorkFolder.PAP.Scripts.Agent.Part
{
    [CreateAssetMenu(fileName = "Important Part Data", menuName = "SO/Important Part Data", order = 0)]
    public class ImportantPartDataSO : AbstractPartDataSO
    {
        [SerializeField] private AnimationCurve zCurve;
        public override void ChangePart(Transform target, Vector3 dir,Transform moveTarget,Vector3 bindPosition, float strength)
        {
            moveTarget.localPosition = bindPosition + new Vector3(0, -strength * 0.5f, 0);
            if (dir == Vector3.zero)
            {
                target.localRotation = Quaternion.identity;
                return;
            }
            Quaternion baseRot = Quaternion.FromToRotation(Vector3.forward,dir);
            
            target.localRotation = baseRot * Quaternion.Euler(-25f, 0, 0);
        }
    }
}