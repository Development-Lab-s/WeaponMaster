using UnityEngine;

namespace WorkFolder.PAP.Scripts.Agent.Part
{
    [CreateAssetMenu(fileName = "Important Part Data", menuName = "SO/Important Part Data", order = 0)]
    public class ImportantPartDataSO : AbstractPartDataSO
    {
        [SerializeField] private AnimationCurve zCurve;
        public override void ChangePart(Transform target, Vector3 dir,Transform moveTarget,Vector3 bindPosition, float strength, Transform weaponTrm)
        {
            moveTarget.localPosition = bindPosition + new Vector3(0, -strength * 1f, 0);
            if (dir == Vector3.zero)
            {
                target.localRotation = Quaternion.identity;
                return;
            }
            Quaternion baseRot = Quaternion.FromToRotation(Vector3.forward,dir);
            
            target.localRotation = baseRot * Quaternion.Euler(-25f, 0, 0);
            
            Vector3 aimAxis = -weaponTrm.forward;

            Vector3 toPlayer = -dir;
            Vector3 reference = Vector3.ProjectOnPlane(toPlayer, aimAxis);

            Vector3 weaponUp = Vector3.ProjectOnPlane(weaponTrm.up, aimAxis);

            if (reference.sqrMagnitude > 1e-6f && weaponUp.sqrMagnitude > 1e-6f)
            {
                float rollError = Vector3.SignedAngle(weaponUp, reference, aimAxis);
                weaponTrm.rotation = Quaternion.AngleAxis(rollError, aimAxis) * weaponTrm.rotation;
            }
        }
    }
}