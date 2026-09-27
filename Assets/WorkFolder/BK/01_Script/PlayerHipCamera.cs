using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class PlayerHipCamera : MonoBehaviour
{
   [Header("필수: 씬의 플레이어 최상위 오브젝트")]
    public Transform playerRoot;
    [Header("선택: 자동 검색이 잘못된 경우에만 지정")]
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private Transform hipsOverride;
    [SerializeField] private Transform forwardReference;
    [Header("이동 시간 (초, 0이면 즉시)")]
    [Min(0f)] [SerializeField] private float orbitDuration = 2.5f;
    [Min(0f)] [SerializeField] private float hipMoveDuration = 1f;
    [Min(0f)] [SerializeField] private float returnDuration = 1f;
    [Header("한 바퀴 회전 구도 (최종 골반 구도와 별도)")]
    [Min(0f)] [SerializeField] private float orbitEntryDuration = 0.6f;
    [Min(0.05f)] [SerializeField] private float orbitDistanceRatio = 1f;
    [SerializeField] private float orbitHeightRatio = 0.15f;
    [SerializeField] private float orbitLookHeightRatio = 0.1f;
    [Header("캐릭터 크기에 대한 비율")]
    [Min(0.05f)] [SerializeField] private float distanceRatio = 0.55f;
    [SerializeField] private float heightRatio = 0f;
    [SerializeField] private float sideRatio = 0f;
    [SerializeField] private float lookHeightRatio = 0f;
    [SerializeField] private bool reverseForward;
    [Header("전환 중 중지할 기존 카메라 제어 스크립트")]
    [SerializeField] private Behaviour[] cameraControllers;
    [Header("UI 이벤트")]
    public UnityEvent onHipViewEntered = new UnityEvent();
    public UnityEvent onHipViewExited = new UnityEvent();

    private enum State { Normal, MovingToOrbit, Orbiting, MovingToHip, Hip, Returning }
    private State state;
    public bool CanSelectHip => state == State.Hip;
    private Transform hips, leftLeg, rightLeg, head;
    private float bodySize, elapsed;
    private Vector3 originalPosition, phasePosition, orbitOffset;
    private Vector3 orbitAxis;
    private Quaternion originalRotation, phaseRotation;
    private bool[] controllerStates;
    private CursorLockMode savedCursorLock;
    private bool savedCursorVisible, sessionActive;
    private CinemachineBrain _brain;
 public static bool IsCameraEnable = false;

    private Vector3 Up => playerRoot.up.normalized;
    private Vector3 Forward
    {
        get
        {
            Vector3 direction;
            if (forwardReference != null)
                direction = forwardReference.forward;
            else if (leftLeg != null && rightLeg != null)
                // Anatomical right cross up = forward. Do not use pelvis.forward:
                // FBX bone local axes often differ from the character's facing.
                direction = Vector3.Cross(rightLeg.position - leftLeg.position, Up);
            else
                direction = playerRoot.forward;
            direction = Vector3.ProjectOnPlane(direction, Up);
            if (direction.sqrMagnitude < 0.000001f)
                direction = Vector3.ProjectOnPlane(playerRoot.forward, Up);
            return direction.normalized * (reverseForward ? -1f : 1f);
        }
    }
    private Vector3 HipTarget => hips.position + Up * (bodySize * lookHeightRatio);
    private Vector3 OrbitTarget => hips.position + orbitAxis * (bodySize * orbitLookHeightRatio);
    private Vector3 HipPosition => hips.position
        - Forward * (bodySize * Mathf.Max(0.05f, distanceRatio))
        + Up * (bodySize * heightRatio)
        + Vector3.Cross(Up, Forward) * (bodySize * sideRatio);

    private void Awake()
    {
        _brain = GetComponent<CinemachineBrain>();
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            return;
        if (state == State.Normal) EnterHipView();
        else if (state == State.Hip) ReturnToNormal();
        // Ignore input during transitions. UI clicks never trigger a return.
    }

    public void EnterHipView()
    {
        if (state != State.Normal || !ResolveBones()) return;
        _brain.enabled = false;
        originalPosition = playerRoot.InverseTransformPoint(transform.position);
        originalRotation = Quaternion.Inverse(playerRoot.rotation) * transform.rotation;
        IsCameraEnable = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        sessionActive = true;
        controllerStates = new bool[cameraControllers == null ? 0 : cameraControllers.Length];
        for (int i = 0; i < controllerStates.Length; i++)
        {
            Behaviour control = cameraControllers[i];
            if (control == null || control == this || control is Animator || control is Camera) continue;
            bool duplicate = false;
            for (int j = 0; j < i; j++)
                if (cameraControllers[j] == control) duplicate = true;
            if (duplicate) continue;
            controllerStates[i] = control.enabled;
            control.enabled = false;
        }

        // Keep only the initial horizontal direction, not the camera's distance
        // or height. Freeze the orbit plane so turning the player cannot add
        // extra rotation to the camera's one full revolution.
        orbitAxis = Up;
        Vector3 radial = Vector3.ProjectOnPlane(transform.position - hips.position, orbitAxis);
        if (radial.sqrMagnitude < 0.000001f) radial = -Forward;
        orbitOffset = radial.normalized * (bodySize * Mathf.Max(0.05f, orbitDistanceRatio))
            + orbitAxis * (bodySize * orbitHeightRatio);
        Begin(orbitDuration <= 0f ? State.MovingToHip : State.MovingToOrbit);
    }

    public void ReturnToNormal()
    {
        if (!sessionActive || state == State.Returning) return;
        IsCameraEnable = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        bool wasHip = state == State.Hip;
        Begin(State.Returning);
        if (wasHip)
        {
            onHipViewExited.Invoke();
            _brain.enabled = true;
        }
    }

    private void Begin(State next)
    {
        phasePosition = playerRoot.InverseTransformPoint(transform.position);
        phaseRotation = Quaternion.Inverse(playerRoot.rotation) * transform.rotation;
        elapsed = 0f;
        state = next;
    }

    private void LateUpdate()
    {
        if (!sessionActive) return;
        if (playerRoot == null || hips == null)
        {
            bool wasHip = state == State.Hip;
            RestoreControl();
            if (wasHip)
            {
                onHipViewExited.Invoke();
            }
            return;
        }
        elapsed += Time.unscaledDeltaTime;
        if (state == State.MovingToOrbit)
        {
            float t = Progress(orbitEntryDuration);
            Vector3 position = hips.position + orbitOffset;
            BlendTo(position, LookRotation(position, OrbitTarget), t);
            if (t >= 1f) Begin(State.Orbiting);
        }
        else if (state == State.Orbiting)
        {
            float t = Progress(orbitDuration);
            transform.position = hips.position
                + Quaternion.AngleAxis(360f * Mathf.SmoothStep(0f, 1f, t), orbitAxis) * orbitOffset;
            transform.rotation = LookRotation(transform.position, OrbitTarget);
            if (t >= 1f) Begin(State.MovingToHip);
        }
        else if (state == State.MovingToHip)
        {
            float t = Progress(hipMoveDuration);
            BlendTo(HipPosition, LookRotation(HipPosition, HipTarget), t);
            if (t >= 1f)
            {
                state = State.Hip;
                onHipViewEntered.Invoke();
            }
        }
        else if (state == State.Hip)
        {
            transform.SetPositionAndRotation(HipPosition, LookRotation(HipPosition, HipTarget));
        }
        else if (state == State.Returning)
        {
            float t = Progress(returnDuration);
            BlendTo(playerRoot.TransformPoint(originalPosition), playerRoot.rotation * originalRotation, t);
            if (t >= 1f) RestoreControl();
        }
    }

    private float Progress(float duration) => duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
    private void BlendTo(Vector3 position, Quaternion rotation, float t)
    {
        t = Mathf.SmoothStep(0f, 1f, t);
        transform.SetPositionAndRotation(
            Vector3.Lerp(playerRoot.TransformPoint(phasePosition), position, t),
            Quaternion.Slerp(playerRoot.rotation * phaseRotation, rotation, t));
    }
    private Quaternion LookRotation(Vector3 position, Vector3 target)
    {
        Vector3 direction = target - position;
        if (direction.sqrMagnitude < 0.000001f) return transform.rotation;
        if (Vector3.Cross(direction, Up).sqrMagnitude < 0.000001f) return transform.rotation;
        return Quaternion.LookRotation(direction, Up);
    }

    private bool ResolveBones()
    {
        if (playerRoot == null || playerRoot == transform || playerRoot.IsChildOf(transform))
        {
            Debug.LogError("PlayerHipCamera: Player Root에 카메라가 아닌 씬의 플레이어 루트를 연결하세요.", this);
            return false;
        }
        hips = hipsOverride;
        leftLeg = rightLeg = head = null;
        Animator selected = characterAnimator;
        if (selected == null)
        {
            foreach (Animator candidate in playerRoot.GetComponentsInChildren<Animator>(true))
            {
                if (candidate.avatar != null && candidate.avatar.isValid && candidate.avatar.isHuman)
                { selected = candidate; break; }
            }
        }
        if (selected != null && selected.avatar != null && selected.avatar.isValid && selected.avatar.isHuman)
        {
            if (hips == null) hips = selected.GetBoneTransform(HumanBodyBones.Hips);
            leftLeg = selected.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            rightLeg = selected.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            head = selected.GetBoneTransform(HumanBodyBones.Head);
        }
        Transform searchRoot = selected != null ? selected.transform : playerRoot;
        Transform[] bones = searchRoot.GetComponentsInChildren<Transform>(true);
        if (hips == null) hips = FindBone(bones, "root", "hips", "hip");
        if (leftLeg == null) leftLeg = FindBone(bones, "thighl", "leftupleg", "leftupperleg");
        if (rightLeg == null) rightLeg = FindBone(bones, "thighr", "rightupleg", "rightupperleg");
        if (head == null) head = FindBone(bones, "head");
        if (hips == null)
        {
            Debug.LogError("PlayerHipCamera: 골반을 찾지 못했습니다. Player Root의 자식에 모델이 있는지 확인하거나 Hips Override에 pelvis를 연결하세요.", this);
            return false;
        }
        // Twice pelvis-to-head distance provides an animation-independent cached
        // scale for this visit, without depending on the FBX root's import scale.
        bodySize = head != null ? Vector3.Distance(hips.position, head.position) * 2f : 0f;
        if (bodySize < 0.01f)
        {
            foreach (SkinnedMeshRenderer mesh in playerRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Vector3 size = mesh.bounds.size;
                bodySize = Mathf.Max(bodySize, size.x, size.y, size.z);
            }
        }
        if (bodySize < 0.01f) bodySize = 1.8f;
        if (forwardReference == null && (leftLeg == null || rightLeg == null))
            Debug.LogWarning("PlayerHipCamera: 양쪽 허벅지 뼈를 찾지 못해 Player Root의 앞 방향을 사용합니다. 필요하면 Forward Reference를 연결하세요.", this);
        return true;
    }

    private static Transform FindBone(Transform[] bones, params string[] names)
    {
        foreach (string name in names)
        foreach (Transform bone in bones)
        {
            string key = bone.name.ToLowerInvariant();
            int colon = key.LastIndexOf(':');
            if (colon >= 0) key = key.Substring(colon + 1);
            key = key.Replace(".", "").Replace("_", "").Replace("-", "").Replace(" ", "");
            if (key == name) return bone;
        }
        return null;
    }

    private void RestoreControl()
    {
        if (!sessionActive) return;
        sessionActive = false;
        state = State.Normal;
        if (cameraControllers != null && controllerStates != null)
        {
            for (int i = 0; i < Mathf.Min(cameraControllers.Length, controllerStates.Length); i++)
                if (cameraControllers[i] != null && controllerStates[i]) cameraControllers[i].enabled = true;
        }
    }
    private void OnDisable()
    {
        bool wasHip = state == State.Hip;
        if (sessionActive && playerRoot != null)
            transform.SetPositionAndRotation(playerRoot.TransformPoint(originalPosition), playerRoot.rotation * originalRotation);
        RestoreControl();
        if (wasHip) onHipViewExited.Invoke();
    }
}