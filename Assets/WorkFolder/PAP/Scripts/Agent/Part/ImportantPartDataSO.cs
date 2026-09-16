using UnityEngine;

namespace WorkFolder.PAP.Scripts.Agent.Part
{
    [CreateAssetMenu(fileName = "Important Part Data", menuName = "SO/Important Part Data", order = 0)]
    public class ImportantPartDataSO : AbstractPartDataSO
    {
        [SerializeField] private AnimationCurve zCurve;
        public override void ChangePart(Transform target, Vector3 dir,Transform moveTarget, float strength)
        {
            if (dir == Vector3.zero)
            {
                target.localRotation = Quaternion.identity;
                return;
            }
            Quaternion baseRot = Quaternion.FromToRotation(Vector3.forward,dir);
            
            moveTarget.localPosition = new Vector3(0, -strength * 1f, 0);
            target.localRotation = baseRot * Quaternion.Euler(-25f, 0, 0);
        }
    }
}