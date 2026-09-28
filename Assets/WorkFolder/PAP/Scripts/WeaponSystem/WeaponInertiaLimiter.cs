using UnityEngine;

namespace WorkFolder.PAP.Scripts.WeaponSystem
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class WeaponInertiaLimiter : MonoBehaviour
    {
        private const int SettleSteps = 2;

        private Rigidbody _body;
        private Rigidbody _partBody;
        private Joint _joint;
        private float _maximumRatio;
        private int _stepsToWait;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
        }

        public void Bind(Joint joint, Rigidbody partBody, float maximumRatio)
        {
            _joint = joint;
            _partBody = partBody;
            _maximumRatio = Mathf.Max(1f, maximumRatio);
            _stepsToWait = SettleSteps;
            enabled = true;
        }

        private void FixedUpdate()
        {
            if (_joint == null || _partBody == null)
            {
                enabled = false;
                return;
            }

            if (_stepsToWait-- > 0)
                return;

            Vector3 pivot = GetPivot(_partBody);
            float weaponInertia = PivotInertia(_body, pivot);
            float partInertia = Mathf.Max(PivotInertia(_partBody, pivot), 0.0001f);
            float ratio = weaponInertia / partInertia;
            float massScale = Mathf.Max(1f, ratio / _maximumRatio);

            _joint.massScale = massScale;
            Debug.Log(
                $"[WeaponInertiaLimiter] {name}: {_partBody.name} 대비 회전 관성 {ratio:F1}배 " +
                $"(상한 {_maximumRatio:F1}) → massScale {massScale:F2}",
                this);

            enabled = false;
        }

        private static Vector3 GetPivot(Rigidbody partBody)
        {
            return partBody.TryGetComponent(out ConfigurableJoint partJoint)
                ? partBody.transform.TransformPoint(partJoint.anchor)
                : partBody.worldCenterOfMass;
        }

        private static float PivotInertia(Rigidbody body, Vector3 pivot)
        {
            Vector3 tensor = body.inertiaTensor;
            float own = Mathf.Max(tensor.x, Mathf.Max(tensor.y, tensor.z));
            return own + body.mass * (body.worldCenterOfMass - pivot).sqrMagnitude;
        }
    }
}
