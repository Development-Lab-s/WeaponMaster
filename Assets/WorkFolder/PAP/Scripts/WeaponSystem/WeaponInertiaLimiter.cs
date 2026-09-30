using UnityEngine;

namespace WorkFolder.PAP.Scripts.WeaponSystem
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class WeaponInertiaLimiter : MonoBehaviour
    {
        private const int SettleSteps = 2;

        // 머리처럼 가볍고 관절에 바짝 붙은 부위에 긴 무기를 달면 관성 비율이 100배를 넘어
        // massScale도 10을 넘는다. 조인트만 무기를 가볍게 보고 충돌·중력은 원래 무게로 계산하니
        // 부딪히는 순간 에너지가 생겨 무기가 계속 돈다. 주인 화면의 물리만 이렇게 되고, 다른
        // 화면은 퍼펫이 루트에 끌려가 매번 제자리로 돌아오므로 멀쩡해 보인다.
        private const float MaximumMassScale = 3f;
        // 대검 끝이 30 m/s로 휘둘러도 약 8 rad/s다. 그 이상은 폭주로 보고 막는다.
        private const float MaximumAngularSpeed = 25f;

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

            ConditionInertiaTensor(_body);

            Vector3 pivot = GetPivot(_partBody);
            float weaponInertia = PivotInertia(_body, pivot);
            float partInertia = Mathf.Max(PivotInertia(_partBody, pivot), 0.0001f);
            float ratio = weaponInertia / partInertia;
            float massScale = Mathf.Clamp(ratio / _maximumRatio, 1f, MaximumMassScale);

            _joint.massScale = massScale;
            _body.maxAngularVelocity = MaximumAngularSpeed;
            Debug.Log(
                $"[WeaponInertiaLimiter] {name}: {_partBody.name} 대비 회전 관성 {ratio:F1}배 " +
                $"(상한 {_maximumRatio:F1}) → massScale {massScale:F2}",
                this);

            enabled = false;
        }

        // 창처럼 가늘고 긴 무기는 콜라이더로 계산한 관성이 긴 축 방향만 극단적으로 작다
        // (창은 약 1 : 2800). 날 세우기가 그 축의 회전을 풀어 두기 때문에, 창날이 축에서 조금만
        // 벗어나 있어도 작은 힘에 그 축으로 마구 돌고, 비틀기 힘이 되잡으며 덜덜 떤다. PhysX도 이런
        // 극단적인 비율에서 불안정하다. 가장 큰 값의 일정 비율 아래인 축만 그 비율까지 올린다.
        private const float MinimumInertiaRatio = 0.05f;

        private void ConditionInertiaTensor(Rigidbody body)
        {
            Vector3 tensor = body.inertiaTensor;
            float floor = Mathf.Max(tensor.x, Mathf.Max(tensor.y, tensor.z)) * MinimumInertiaRatio;
            Vector3 conditioned = new Vector3(
                Mathf.Max(tensor.x, floor),
                Mathf.Max(tensor.y, floor),
                Mathf.Max(tensor.z, floor));

            if (conditioned == tensor)
                return;

            Quaternion tensorRotation = body.inertiaTensorRotation;
            body.inertiaTensor = conditioned;
            body.inertiaTensorRotation = tensorRotation;
            Debug.Log($"[WeaponInertiaLimiter] {name}: 관성 보정 {tensor} → {conditioned}", this);
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
