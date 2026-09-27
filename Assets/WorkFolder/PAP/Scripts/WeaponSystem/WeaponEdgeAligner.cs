using UnityEngine;

namespace WorkFolder.PAP.Scripts.WeaponSystem
{
    // Half Sword처럼 휘두르는 방향으로 날이 향하게, 무기를 자기 축(손잡이 → 날끝)으로만 비튼다.
    //
    // 무기는 척추에 조인트로 고정돼 있어서 지금까지는 날을 돌리려면 몸통 전체를 굴려야 했다.
    // 여기서는 무기 조인트의 비틀림 한 축만 풀고, 날이 실제로 움직이는 방향을 향하도록
    // 그 축에만 토크를 준다. 몸통 회전, 조준, 막기는 기존 그대로다.
    //
    // 날 중심점의 속도는 척추를 축으로 도는 호의 접선이므로, 날이 이 속도를 향하면
    // 날의 평면이 휘두르는 호의 평면이 되고 그 평면은 회전 중심(몸통)을 지난다.
    // 입력이 아니라 물리 결과를 보고 판단하므로 소유자와 프록시 모두 같은 규칙으로 돈다.
    [RequireComponent(typeof(Rigidbody), typeof(ConfigurableJoint))]
    public sealed class WeaponEdgeAligner : MonoBehaviour
    {
        private const string BladeTag = "Weapon";

        public enum EdgeMode
        {
            Auto,
            DoubleEdged,
            SingleEdged
        }

        [Header("Swing Detection")]
        // 날 중심점이 몸 기준으로 이보다 빨라야 휘두르는 중으로 본다. 느리면 마지막 방향을 유지한다.
        [SerializeField, Min(0f)] private float minimumSwingSpeed = 3f;
        // 목표 날 방향이 바뀌는 최대 속도(도/초).
        [SerializeField, Min(0f)] private float rollSpeed = 900f;

        [Header("Twist Drive")]
        [SerializeField, Min(0f)] private float twistSpring = 800f;
        [SerializeField, Range(0f, 2f)] private float twistDampingRatio = 1f;
        [SerializeField, Min(0f)] private float maximumTwistAcceleration = 3000f;

        [Header("Edge")]
        // Auto: 날이 손잡이 축에서 한쪽으로 치우쳐 있으면(낫) 외날, 아니면(검, 창) 양날.
        [SerializeField] private EdgeMode edgeMode = EdgeMode.Auto;
        // 외날일 때 반대쪽 날을 앞세운다.
        [SerializeField] private bool flipEdge;
        // 자동 판별이 날 대신 넓은 면을 앞세우는 모델이면 켠다.
        [SerializeField] private bool swapEdgeAndFlat;

        private Rigidbody _weaponBody;
        private Rigidbody _partBody;
        private Rigidbody _characterBody;
        private Vector3 _bladeAxisLocal;
        private Vector3 _edgeAxisLocal;
        private Vector3 _bladeCenterLocal;
        private bool _singleEdged;
        private bool _isBound;
        private bool _hasReference;
        private Vector3 _referenceEdgeInPart;
        private float _targetRoll;

        private void Awake()
        {
            _weaponBody = GetComponent<Rigidbody>();
        }

        // 조인트의 connectedBody를 연결하기 직전에 호출해야 한다. 조인트는 연결되는 순간의
        // 축 설정과 상대 자세를 기준으로 삼는다.
        public void Bind(ConfigurableJoint joint, Rigidbody partBody, Rigidbody characterBody)
        {
            _partBody = partBody;
            _characterBody = characterBody;
            _hasReference = false;

            if (joint == null || partBody == null || !TryDetectBlade())
            {
                _isBound = false;
                enabled = false;
                return;
            }

            joint.axis = _bladeAxisLocal;
            joint.secondaryAxis = _edgeAxisLocal;
            joint.angularXMotion = ConfigurableJointMotion.Free;
            _isBound = true;
            enabled = true;
        }

        private void FixedUpdate()
        {
            if (!_isBound || _partBody == null)
                return;

            Vector3 bladeAxis = transform.TransformDirection(_bladeAxisLocal).normalized;
            Vector3 edge = Vector3.ProjectOnPlane(
                transform.TransformDirection(_edgeAxisLocal),
                bladeAxis).normalized;

            // 비틀림 각도를 잴 기준. 척추에 붙은 방향이라 몸통이 돌면 날의 유지 방향도 같이 돈다.
            if (!_hasReference)
            {
                _referenceEdgeInPart = Quaternion.Inverse(_partBody.rotation) * edge;
                _targetRoll = 0f;
                _hasReference = true;
            }

            Vector3 referenceEdge = Vector3.ProjectOnPlane(
                _partBody.rotation * _referenceEdgeInPart,
                bladeAxis);
            if (referenceEdge.sqrMagnitude < 0.0001f)
                return;
            referenceEdge.Normalize();

            float currentRoll = Vector3.SignedAngle(referenceEdge, edge, bladeAxis);
            UpdateTargetRoll(referenceEdge, bladeAxis);
            ApplyTwist(bladeAxis, currentRoll);
        }

        private void UpdateTargetRoll(Vector3 referenceEdge, Vector3 bladeAxis)
        {
            Vector3 bladeCenter = transform.TransformPoint(_bladeCenterLocal);
            Vector3 velocity = _weaponBody.GetPointVelocity(bladeCenter);

            // 걷기로 생긴 이동은 휘두르기가 아니다. 프록시 루트는 kinematic이라 0이고,
            // 프록시 무기는 루트에 끌려가 속도에 이동이 들어있지 않으므로 그대로 맞는다.
            if (_characterBody != null)
                velocity -= _characterBody.linearVelocity;

            Vector3 swing = Vector3.ProjectOnPlane(velocity, bladeAxis);
            if (swing.sqrMagnitude < minimumSwingSpeed * minimumSwingSpeed)
                return;

            float desiredRoll = Vector3.SignedAngle(referenceEdge, swing, bladeAxis);

            if (_singleEdged)
            {
                if (flipEdge)
                    desiredRoll += 180f;
            }
            else
            {
                // 양날은 어느 날이 앞서도 된다. 방향을 바꿔 휘둘러도 180도 뒤집지 않게 가까운 쪽을 고른다.
                float opposite = desiredRoll + 180f;
                if (Mathf.Abs(Mathf.DeltaAngle(_targetRoll, opposite))
                    < Mathf.Abs(Mathf.DeltaAngle(_targetRoll, desiredRoll)))
                    desiredRoll = opposite;
            }

            _targetRoll = Mathf.MoveTowardsAngle(
                _targetRoll,
                desiredRoll,
                rollSpeed * Time.fixedDeltaTime);
        }

        private void ApplyTwist(Vector3 bladeAxis, float currentRoll)
        {
            float error = Mathf.DeltaAngle(currentRoll, _targetRoll) * Mathf.Deg2Rad;
            float twistVelocity = Vector3.Dot(
                _weaponBody.angularVelocity - _partBody.angularVelocity,
                bladeAxis);
            float damper = 2f * twistDampingRatio * Mathf.Sqrt(twistSpring);
            float acceleration = Mathf.Clamp(
                twistSpring * error - damper * twistVelocity,
                -maximumTwistAcceleration,
                maximumTwistAcceleration);

            _weaponBody.AddTorque(bladeAxis * acceleration, ForceMode.Acceleration);
        }

        // 무기 루트 기준으로 날 축, 날 방향, 외날 여부를 모델 모양에서 찾는다.
        // 날은 무기 Rigidbody에 붙은 "Weapon" 태그 콜라이더다. 없으면(도리깨처럼 날이
        // 다른 Rigidbody에 달린 무기) 비틀 날이 없으므로 이 컴포넌트를 끈다.
        private bool TryDetectBlade()
        {
            MeshFilter bladeMesh = null;
            foreach (Collider candidate in GetComponentsInChildren<Collider>(true))
            {
                if (!candidate.CompareTag(BladeTag)
                    || candidate.attachedRigidbody != _weaponBody)
                    continue;

                bladeMesh = candidate.GetComponent<MeshFilter>();
                if (bladeMesh != null && bladeMesh.sharedMesh != null)
                    break;
            }

            if (bladeMesh == null || bladeMesh.sharedMesh == null)
                return false;

            Bounds weaponBounds = default;
            bool hasWeaponBounds = false;
            foreach (MeshFilter part in GetComponentsInChildren<MeshFilter>(true))
            {
                if (part.sharedMesh == null
                    || part.GetComponentInParent<Rigidbody>() != _weaponBody)
                    continue;

                EncapsulateInRoot(part, ref weaponBounds, ref hasWeaponBounds);
            }

            Bounds bladeBounds = default;
            bool hasBladeBounds = false;
            EncapsulateInRoot(bladeMesh, ref bladeBounds, ref hasBladeBounds);

            // 무기에서 가장 긴 방향이 손잡이 → 날끝이다. 조인트 앵커(루트 원점)에서 무기 쪽을 향하게 한다.
            int bladeIndex = LargestAxis(weaponBounds.size);
            float bladeSign = weaponBounds.center[bladeIndex] < 0f ? -1f : 1f;
            _bladeAxisLocal = AxisVector(bladeIndex) * bladeSign;

            // 날축에 수직인 두 방향 중 날이 더 얇은 쪽이 면, 넓은 쪽이 날이 향하는 방향이다.
            int firstIndex = (bladeIndex + 1) % 3;
            int secondIndex = (bladeIndex + 2) % 3;
            bool firstIsWider = bladeBounds.size[firstIndex] >= bladeBounds.size[secondIndex];
            int edgeIndex = firstIsWider != swapEdgeAndFlat ? firstIndex : secondIndex;

            float bladeOffset = bladeBounds.center[edgeIndex];
            bool offsetBlade = Mathf.Abs(bladeOffset) > bladeBounds.size[edgeIndex] * 0.25f;

            _singleEdged = edgeMode == EdgeMode.SingleEdged
                           || (edgeMode == EdgeMode.Auto && offsetBlade);
            // 외날은 날이 튀어나온 쪽(낫이면 날끝 쪽)이 앞서게 한다.
            float edgeSign = _singleEdged && bladeOffset < 0f ? -1f : 1f;
            _edgeAxisLocal = AxisVector(edgeIndex) * edgeSign;
            _bladeCenterLocal = bladeBounds.center;

            return true;
        }

        private void EncapsulateInRoot(MeshFilter part, ref Bounds bounds, ref bool hasBounds)
        {
            Bounds meshBounds = part.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = meshBounds.center + Vector3.Scale(
                    meshBounds.extents,
                    new Vector3(
                        (i & 1) == 0 ? -1f : 1f,
                        (i & 2) == 0 ? -1f : 1f,
                        (i & 4) == 0 ? -1f : 1f));
                Vector3 point = transform.InverseTransformPoint(
                    part.transform.TransformPoint(corner));

                if (!hasBounds)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(point);
                }
            }
        }

        private static int LargestAxis(Vector3 size)
        {
            if (size.x >= size.y && size.x >= size.z)
                return 0;

            return size.y >= size.z ? 1 : 2;
        }

        private static Vector3 AxisVector(int index)
        {
            Vector3 axis = Vector3.zero;
            axis[index] = 1f;
            return axis;
        }
    }
}
