using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace WorkFolder.PAP.Scripts.WeaponSystem
{
    // 철퇴(도리깨)를 "손잡이에 일정 길이 줄로 묶인 무거운 공"으로 다룬다.
    //
    // 원래 프리팹은 사슬 고리 12개를 HingeJoint 13개로 이었다. 고리마다 한 축(±70°)만 돌 수 있어
    // 사슬이 비틀리지 못하고, 콜라이더가 없는 고리는 관성이 기본값(1,1,1)이라 둔하게 반응했다.
    // 관절이 줄줄이 이어져 오차가 쌓이고, 공(4kg)이 고리(0.2kg)의 20배라 늘어나고 떨렸다.
    // 휘두르면 바로 따라오지 않고 꼬이던 원인이다.
    //
    // 여기서는 공과 손잡이 사이를 거리만 제한하는 조인트 하나(줄)로 바꾸고, 사슬 고리는 물리를
    // 빼고 손잡이와 공 사이 곡선 위에 매 프레임 늘어놓는 장식으로 만든다(스킨 메시 뼈라 메시도 따라온다).
    // 공은 소유자만 물리로 움직이고, 손잡이 기준 위치·회전을 네트워크로 보내 다른 화면은 그대로 따라 그린다.
    [DisallowMultipleComponent]
    public sealed class FlailRope : NetworkBehaviour
    {
        [SerializeField] private Rigidbody ball;

        [Header("Physics")]
        [SerializeField, Min(0.1f)] private float ballMass = 1.5f;
        [SerializeField, Min(0f)] private float ballLinearDamping = 0.05f;
        [SerializeField, Min(0f)] private float ballAngularDamping = 0.5f;
        // 손잡이에 콜라이더가 없어 관성이 기본값(1,1,1)으로 잡히므로 실제 손잡이 크기로 준다.
        [SerializeField, Min(0.0001f)] private float handleInertia = 0.02f;
        // 줄이 팽팽해지는 순간 공이 딱딱한 벽에 부딪히듯 튕기지 않게 약간의 탄성을 준다.
        // 원심력(1.5kg, 초당 1.6바퀴, 반지름 1.5m ≈ 230N)에 늘어나는 길이 ≈ 230/4000 ≈ 6cm.
        [SerializeField, Min(0f)] private float ropeStiffness = 4000f;
        [SerializeField, Min(0f)] private float ropeDamper = 80f;

        [Header("Chain Visual")]
        // 줄이 느슨할 때 사슬이 처지는 정도(1 = 같은 길이의 포물선 처짐).
        [SerializeField, Range(0f, 1.5f)] private float sagAmount = 1f;

        [Networked] private Vector3 NetBallLocalPosition { get; set; }
        [Networked] private Quaternion NetBallLocalRotation { get; set; }
        [Networked] private NetworkBool NetHasBallState { get; set; }

        private readonly List<Transform> _links = new List<Transform>();
        private readonly List<float> _linkFractions = new List<float>();
        private readonly List<Quaternion> _linkRestRotations = new List<Quaternion>();

        private Rigidbody _handle;
        private ConfigurableJoint _rope;
        private Vector3 _handleAttachLocal;
        private float _restLengthLocal;
        private float _appliedScale = -1f;
        private bool _converted;

        public Rigidbody Ball => ball;

        private void Awake()
        {
            _handle = GetComponent<Rigidbody>();
            if (ball == null)
                ball = FindBall();

            ConvertToRope();
        }

        public override void Spawned()
        {
            if (!_converted || HasStateAuthority)
                return;

            // 다른 사람 화면: 공은 소유자가 보낸 값으로만 그린다. 줄 조인트가 남아 있으면
            // 받은 위치와 로컬 물리가 서로 당기므로 없애고 공을 kinematic으로 둔다.
            if (_rope != null)
                Destroy(_rope);
            ball.isKinematic = true;
            ball.interpolation = RigidbodyInterpolation.None;
        }

        public override void FixedUpdateNetwork()
        {
            if (!_converted || !HasStateAuthority)
                return;

            NetBallLocalPosition = transform.InverseTransformPoint(ball.position);
            NetBallLocalRotation = Quaternion.Inverse(transform.rotation) * ball.rotation;
            NetHasBallState = true;
        }

        public override void Render()
        {
            if (!_converted || HasStateAuthority)
                return;

            var interpolator = new NetworkBehaviourBufferInterpolator(this);
            bool hasState = interpolator
                ? interpolator.NetworkBool(nameof(NetHasBallState))
                : NetHasBallState;
            if (!hasState)
                return;

            Vector3 localPosition = interpolator
                ? interpolator.Vector3(nameof(NetBallLocalPosition))
                : NetBallLocalPosition;
            Quaternion localRotation = interpolator
                ? interpolator.Quaternion(nameof(NetBallLocalRotation))
                : NetBallLocalRotation;

            ball.transform.SetPositionAndRotation(
                transform.TransformPoint(localPosition),
                transform.rotation * localRotation);
        }

        private void FixedUpdate()
        {
            // 쌍수 크기 조절 등으로 무기 크기가 바뀌면 줄 길이(월드 단위)도 같이 맞춘다.
            UpdateRopeLength();
        }

        private void LateUpdate()
        {
            if (_converted)
                LayOutLinks();
        }

        private Rigidbody FindBall()
        {
            foreach (Collider candidate in GetComponentsInChildren<Collider>(true))
            {
                Rigidbody body = candidate.attachedRigidbody;
                if (body != null && body != _handle && candidate.CompareTag("Weapon"))
                    return body;
            }

            return null;
        }

        private void ConvertToRope()
        {
            if (ball == null || _handle == null)
            {
                Debug.LogWarning($"[FlailRope] {name}: 공 또는 손잡이 Rigidbody를 찾지 못했습니다.", this);
                return;
            }

            // 공 → 고리 → ... → 손잡이 순으로 조인트를 따라가며 사슬 순서를 알아낸다.
            var chain = new List<Rigidbody>();
            Joint joint = ball.GetComponent<Joint>();
            while (joint != null && joint.connectedBody != null && joint.connectedBody != _handle)
            {
                Rigidbody link = joint.connectedBody;
                if (chain.Contains(link))
                    break;
                chain.Add(link);
                joint = link.GetComponent<Joint>();
            }
            chain.Reverse(); // 손잡이 쪽 → 공 쪽

            // 사슬이 손잡이에 붙는 지점. 첫 고리가 없으면(사슬 없이 공만 매달린 경우) 공 조인트의 연결 지점.
            Vector3 handleAttachWorld = chain.Count > 0
                ? chain[0].position
                : ball.position;
            _handleAttachLocal = transform.InverseTransformPoint(handleAttachWorld);

            // 원래 배치에서 각 고리가 손잡이→공 경로의 어디쯤(비율)에 있는지, 방향은 어떤지 기록한다.
            Vector3 previous = handleAttachWorld;
            var cumulative = new List<float>();
            float length = 0f;
            foreach (Rigidbody link in chain)
            {
                length += Vector3.Distance(previous, link.position);
                cumulative.Add(length);
                previous = link.position;
            }
            length += Vector3.Distance(previous, ball.position);
            if (length < 0.0001f)
            {
                Debug.LogWarning($"[FlailRope] {name}: 사슬 길이가 0입니다.", this);
                return;
            }

            _restLengthLocal = length / Mathf.Max(transform.lossyScale.x, 0.0001f);
            Quaternion restFrame = ChainFrame(ball.position - handleAttachWorld);
            for (int i = 0; i < chain.Count; i++)
            {
                Transform link = chain[i].transform;
                _links.Add(link);
                _linkFractions.Add(cumulative[i] / length);
                _linkRestRotations.Add(Quaternion.Inverse(restFrame) * link.rotation);
            }

            // 고리의 물리를 뺀다. 조인트가 Rigidbody에 의존하므로 조인트부터 지운다.
            foreach (Rigidbody link in chain)
            {
                foreach (Joint linkJoint in link.GetComponents<Joint>())
                    DestroyImmediate(linkJoint);
                DestroyImmediate(link);
            }

            foreach (Joint ballJoint in ball.GetComponents<Joint>())
                DestroyImmediate(ballJoint);

            ball.mass = ballMass;
            ball.linearDamping = ballLinearDamping;
            ball.angularDamping = ballAngularDamping;
            ball.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _handle.inertiaTensor = Vector3.one * handleInertia;

            LockHandleJoint();

            // 거리만 제한하는 줄: 세 축 이동을 Limited로 두면 연결점 중심의 구 안으로 제한된다.
            _rope = ball.gameObject.AddComponent<ConfigurableJoint>();
            _rope.autoConfigureConnectedAnchor = false;
            _rope.connectedBody = _handle;
            _rope.anchor = Vector3.zero;
            _rope.connectedAnchor = _handleAttachLocal;
            _rope.xMotion = ConfigurableJointMotion.Limited;
            _rope.yMotion = ConfigurableJointMotion.Limited;
            _rope.zMotion = ConfigurableJointMotion.Limited;
            _rope.angularXMotion = ConfigurableJointMotion.Free;
            _rope.angularYMotion = ConfigurableJointMotion.Free;
            _rope.angularZMotion = ConfigurableJointMotion.Free;
            _rope.linearLimitSpring = new SoftJointLimitSpring
            {
                spring = ropeStiffness,
                damper = ropeDamper
            };
            _rope.enableCollision = false;

            _converted = true;
            UpdateRopeLength();
        }

        // 프리팹의 손잡이는 HingeJoint의 회전축을 한계 0~0으로 막아 척추에 붙인다. 한계는 넘은 뒤에
        // 되돌리는 방식이라 완전히 잠긴 게 아니어서, 공이 줄을 당길 때마다 손잡이가 넘었다 튕겨
        // 돌아오며 철퇴 전체가 튄다. 다른 무기처럼 모든 축이 잠긴 ConfigurableJoint로 바꾼다.
        // Player가 척추에 연결하기(connectedBody 지정) 전인 Awake에서 바꿔야 한다.
        private void LockHandleJoint()
        {
            Vector3 anchor = Vector3.zero;
            foreach (Joint handleJoint in GetComponents<Joint>())
            {
                if (handleJoint is ConfigurableJoint)
                    return;

                anchor = handleJoint.anchor;
                DestroyImmediate(handleJoint);
            }

            ConfigurableJoint locked = gameObject.AddComponent<ConfigurableJoint>();
            locked.anchor = anchor;
            locked.xMotion = ConfigurableJointMotion.Locked;
            locked.yMotion = ConfigurableJointMotion.Locked;
            locked.zMotion = ConfigurableJointMotion.Locked;
            locked.angularXMotion = ConfigurableJointMotion.Locked;
            locked.angularYMotion = ConfigurableJointMotion.Locked;
            locked.angularZMotion = ConfigurableJointMotion.Locked;
            locked.enableCollision = false;
        }

        private void UpdateRopeLength()
        {
            if (_rope == null)
                return;

            float scale = transform.lossyScale.x;
            if (Mathf.Approximately(scale, _appliedScale))
                return;

            _appliedScale = scale;
            _rope.linearLimit = new SoftJointLimit { limit = _restLengthLocal * scale };
        }

        // 손잡이의 위쪽을 기준으로 사슬 방향 좌표계를 만든다. 고리의 원래 번갈아 꺾인 모양을 유지한다.
        private Quaternion ChainFrame(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.000001f)
                direction = -transform.forward;

            Vector3 up = Vector3.ProjectOnPlane(transform.up, direction);
            if (up.sqrMagnitude < 0.000001f)
                up = Vector3.ProjectOnPlane(transform.right, direction);

            return Quaternion.LookRotation(direction, up);
        }

        // 손잡이와 공 사이를 2차 베지어 곡선으로 잇고, 줄이 느슨하면 중력 방향으로 처지게 한다.
        private void LayOutLinks()
        {
            if (_links.Count == 0)
                return;

            Vector3 start = transform.TransformPoint(_handleAttachLocal);
            Vector3 end = ball.transform.position;
            Vector3 chord = end - start;
            float distance = chord.magnitude;
            float ropeLength = _restLengthLocal * transform.lossyScale.x;

            // 포물선 길이 ≈ d + 8h²/(3d) 에서 처짐 h를 구한다.
            float slack = Mathf.Max(0f, ropeLength - distance);
            float sag = sagAmount * Mathf.Min(
                ropeLength * 0.5f,
                Mathf.Sqrt(3f * Mathf.Max(distance, 0.01f) * slack / 8f));

            Vector3 down = Vector3.ProjectOnPlane(Physics.gravity, chord);
            down = down.sqrMagnitude > 0.000001f ? down.normalized : Vector3.down;
            // 2차 베지어의 가운데 처짐은 제어점 오프셋의 절반이다.
            Vector3 control = (start + end) * 0.5f + down * (sag * 2f);

            for (int i = 0; i < _links.Count; i++)
            {
                float t = _linkFractions[i];
                float u = 1f - t;
                Vector3 position = u * u * start + 2f * u * t * control + t * t * end;
                Vector3 tangent = 2f * u * (control - start) + 2f * t * (end - control);

                _links[i].SetPositionAndRotation(
                    position,
                    ChainFrame(tangent) * _linkRestRotations[i]);
            }
        }
    }
}
