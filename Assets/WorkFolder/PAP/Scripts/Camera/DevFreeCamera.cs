using System.Collections.Generic;
using DevLib.ServiceLocator;
using Fusion;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using WorkFolder.PAP.Scripts.MainSystems;
using WorkFolder.PAP.Scripts.Network;
using PlayerAgent = WorkFolder.PAP.Scripts.Agent.Player.Player;

namespace WorkFolder.PAP.Scripts.Camera
{
    // 시네마틱 촬영용 개발자 프리캠.
    // 공유 모드 마스터 클라이언트가 게임 중에 d-e-v-l-a-b를 차례로 입력하면 켜지고,
    // 같은 순서로 다시 입력하면 원래 카메라로 돌아온다. 켜진 동안 내 캐릭터는 입력을 받지
    // 않고 제자리에 선다. 카메라만 로컬로 움직이므로 네트워크로 보내는 값은 없다.
    //
    // 조작: 마우스 시점 / WASD 이동 / E·Space 위, Q·Ctrl 아래 / Shift 빠르게, Alt 느리게
    //       휠 이동 속도, 우클릭+휠 화각 / Z·C 기울이기 / R 화각·기울기 초기화
    //       G 부드러운 움직임 켜고 끄기 / H 화면 UI 숨기기
    public sealed class DevFreeCamera : MonoBehaviour
    {
        private const string SecretCode = "devlab";

        public static bool IsActive { get; private set; }

        [Header("Move")]
        [SerializeField, Min(0.01f)] private float moveSpeed = 6f;
        [SerializeField, Min(1f)] private float fastMultiplier = 4f;
        [SerializeField, Range(0.01f, 1f)] private float slowMultiplier = 0.2f;
        [SerializeField, Min(0.01f)] private float minimumSpeed = 0.2f;
        [SerializeField, Min(0.01f)] private float maximumSpeed = 80f;

        [Header("Look")]
        [SerializeField, Min(0f)] private float lookSensitivity = 0.1f;
        [SerializeField, Min(0f)] private float rollSpeed = 45f;

        [Header("Cinematic Smoothing (0이면 즉시)")]
        [SerializeField, Min(0f)] private float positionSmoothing = 6f;
        [SerializeField, Min(0f)] private float rotationSmoothing = 10f;

        [Header("Lens")]
        [SerializeField, Range(1f, 179f)] private float minimumFieldOfView = 10f;
        [SerializeField, Range(1f, 179f)] private float maximumFieldOfView = 100f;

        private readonly List<Canvas> _hiddenCanvases = new List<Canvas>();
        private string _typed = string.Empty;

        private UnityEngine.Camera _camera;
        private CinemachineBrain _brain;
        private bool _brainWasEnabled;
        private float _originalFieldOfView;
        private CursorLockMode _originalCursorLock;
        private bool _originalCursorVisible;

        private Vector3 _targetPosition;
        private float _yaw;
        private float _pitch;
        private float _roll;
        private float _speed;
        private bool _smoothing = true;
        private bool _uiHidden;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsActive = false;
        }

        // 씬이나 프리팹을 건드리지 않도록 첫 씬이 뜰 때 스스로 하나 만든다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateInstance()
        {
            if (FindAnyObjectByType<DevFreeCamera>() != null)
                return;

            var host = new GameObject(nameof(DevFreeCamera));
            DontDestroyOnLoad(host);
            host.AddComponent<DevFreeCamera>();
        }

        private void OnDisable()
        {
            if (IsActive)
                Exit();
        }

        private void Update()
        {
            ReadSecretCode();

            if (!IsActive)
                return;

            if (_camera == null)
            {
                Exit();
                return;
            }

            float deltaTime = Time.unscaledDeltaTime;
            HandleToggles();
            UpdateLook(deltaTime);
            UpdateMove(deltaTime);
            ApplyToCamera(deltaTime);
        }

        // 글자 입력(onTextInput)이 아니라 물리 키를 읽는다. 한글 입력 모드에서는 d-e-v-l-a-b가
        // "ㅇㄷㅍㅣㅁㅠ"로 들어와 글자 비교로는 절대 맞지 않기 때문이다. 알파벳 키만 기록한다.
        private void ReadSecretCode()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.anyKey.wasPressedThisFrame)
                return;
            if (IsTypingIntoInputField())
                return;

            foreach (KeyControl key in keyboard.allKeys)
            {
                if (key == null
                    || !key.wasPressedThisFrame
                    || key.keyCode < Key.A
                    || key.keyCode > Key.Z)
                    continue;

                _typed += (char)('a' + (key.keyCode - Key.A));
                if (_typed.Length > SecretCode.Length)
                    _typed = _typed.Substring(_typed.Length - SecretCode.Length);

                if (_typed != SecretCode)
                    continue;

                _typed = string.Empty;
                if (IsActive)
                    Exit();
                else
                    TryEnter();
                return;
            }
        }

        private static bool IsTypingIntoInputField()
        {
            EventSystem eventSystem = EventSystem.current;
            GameObject selected = eventSystem != null
                ? eventSystem.currentSelectedGameObject
                : null;
            if (selected == null)
                return false;

            if (selected.TryGetComponent(out TMP_InputField tmpInput) && tmpInput.isFocused)
                return true;

            return selected.TryGetComponent(out UnityEngine.UI.InputField legacyInput)
                && legacyInput.isFocused;
        }

        private void TryEnter()
        {
            if (PlayerHipCamera.IsCameraEnable)
            {
                Debug.Log("[DevFreeCamera] 골반 카메라(Esc) 중에는 켤 수 없습니다.");
                return;
            }

            NetworkRunner runner = FindRunningRunner();
            if (runner == null)
            {
                Debug.Log("[DevFreeCamera] 실행 중인 NetworkRunner가 없습니다.");
                return;
            }

            if (!runner.IsSharedModeMasterClient && !runner.IsServer)
            {
                Debug.Log("[DevFreeCamera] 마스터 클라이언트만 사용할 수 있습니다.");
                return;
            }

            if (!HasSpawnedLocalPlayer())
            {
                Debug.Log("[DevFreeCamera] 게임 중(내 캐릭터가 스폰된 뒤)에만 사용할 수 있습니다.");
                return;
            }

            _camera = ResolveCamera();
            if (_camera == null)
            {
                Debug.Log("[DevFreeCamera] 메인 카메라를 찾지 못했습니다.");
                return;
            }

            // Cinemachine이 매 프레임 카메라를 덮어쓰므로 켜진 동안만 멈춘다.
            _brain = _camera.GetComponent<CinemachineBrain>();
            _brainWasEnabled = _brain != null && _brain.enabled;
            if (_brain != null)
                _brain.enabled = false;

            _originalFieldOfView = _camera.fieldOfView;
            _originalCursorLock = Cursor.lockState;
            _originalCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Transform cameraTransform = _camera.transform;
            Vector3 euler = cameraTransform.rotation.eulerAngles;
            _targetPosition = cameraTransform.position;
            _yaw = euler.y;
            _pitch = Mathf.DeltaAngle(0f, euler.x);
            _roll = 0f;
            _speed = moveSpeed;
            _smoothing = true;

            IsActive = true;
            Debug.Log("[DevFreeCamera] 켜짐. 다시 devlab을 입력하면 돌아갑니다.");
        }

        private void Exit()
        {
            IsActive = false;
            ShowUi();

            if (_camera != null)
                _camera.fieldOfView = _originalFieldOfView;
            if (_brain != null)
                _brain.enabled = _brainWasEnabled;

            Cursor.lockState = _originalCursorLock;
            Cursor.visible = _originalCursorVisible;

            _camera = null;
            _brain = null;
            Debug.Log("[DevFreeCamera] 꺼짐.");
        }

        private void HandleToggles()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.gKey.wasPressedThisFrame)
                _smoothing = !_smoothing;

            if (keyboard.hKey.wasPressedThisFrame)
            {
                if (_uiHidden)
                    ShowUi();
                else
                    HideUi();
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                _camera.fieldOfView = _originalFieldOfView;
                _roll = 0f;
            }
        }

        private void UpdateLook(float deltaTime)
        {
            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;

            if (mouse != null)
            {
                Vector2 look = mouse.delta.ReadValue() * lookSensitivity;
                _yaw += look.x;
                _pitch = Mathf.Clamp(_pitch - look.y, -89f, 89f);

                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    float notches = Mathf.Sign(scroll);
                    if (mouse.rightButton.isPressed)
                        _camera.fieldOfView = Mathf.Clamp(
                            _camera.fieldOfView * Mathf.Pow(0.9f, notches),
                            minimumFieldOfView,
                            maximumFieldOfView);
                    else
                        _speed = Mathf.Clamp(
                            _speed * Mathf.Pow(1.25f, notches),
                            minimumSpeed,
                            maximumSpeed);
                }
            }

            if (keyboard != null)
            {
                float roll = (keyboard.zKey.isPressed ? 1f : 0f)
                           - (keyboard.cKey.isPressed ? 1f : 0f);
                _roll += roll * rollSpeed * deltaTime;
            }
        }

        private void UpdateMove(float deltaTime)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            Vector3 input = new Vector3(
                (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                (keyboard.eKey.isPressed || keyboard.spaceKey.isPressed ? 1f : 0f)
                    - (keyboard.qKey.isPressed || keyboard.leftCtrlKey.isPressed ? 1f : 0f),
                (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));
            if (input.sqrMagnitude < 0.0001f)
                return;

            float speed = _speed;
            if (keyboard.leftShiftKey.isPressed)
                speed *= fastMultiplier;
            if (keyboard.leftAltKey.isPressed)
                speed *= slowMultiplier;

            // 앞뒤·좌우는 보는 방향 기준, 위아래는 월드 기준이라 고개를 숙여도 수직으로 오르내린다.
            Quaternion view = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 planar = view * new Vector3(input.x, 0f, input.z);
            Vector3 move = Vector3.ClampMagnitude(planar + Vector3.up * input.y, 1f);
            _targetPosition += move * (speed * deltaTime);
        }

        private void ApplyToCamera(float deltaTime)
        {
            Transform cameraTransform = _camera.transform;
            Quaternion targetRotation = Quaternion.Euler(_pitch, _yaw, _roll);

            if (!_smoothing)
            {
                cameraTransform.SetPositionAndRotation(_targetPosition, targetRotation);
                return;
            }

            float positionBlend = Blend(positionSmoothing, deltaTime);
            float rotationBlend = Blend(rotationSmoothing, deltaTime);
            cameraTransform.SetPositionAndRotation(
                Vector3.Lerp(cameraTransform.position, _targetPosition, positionBlend),
                Quaternion.Slerp(cameraTransform.rotation, targetRotation, rotationBlend));
        }

        private static float Blend(float response, float deltaTime)
        {
            return response <= 0f ? 1f : 1f - Mathf.Exp(-response * deltaTime);
        }

        private void HideUi()
        {
            _hiddenCanvases.Clear();
            foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas || !canvas.enabled)
                    continue;

                canvas.enabled = false;
                _hiddenCanvases.Add(canvas);
            }

            _uiHidden = true;
        }

        private void ShowUi()
        {
            foreach (Canvas canvas in _hiddenCanvases)
            {
                if (canvas != null)
                    canvas.enabled = true;
            }

            _hiddenCanvases.Clear();
            _uiHidden = false;
        }

        private void OnGUI()
        {
            if (!IsActive || _uiHidden || _camera == null)
                return;

            GUI.Label(
                new Rect(12f, 12f, 520f, 22f),
                $"DEV FREECAM  속도 {_speed:0.0}  화각 {_camera.fieldOfView:0}  기울기 {_roll:0}  부드럽게 {(_smoothing ? "ON" : "OFF")}  (H: UI 숨김)");
        }

        private static NetworkRunner FindRunningRunner()
        {
            NetworkRunner current = RunnerProvider.Current;
            if (current != null && current.IsRunning)
                return current;

            foreach (NetworkRunner runner in NetworkRunner.Instances)
            {
                if (runner != null && runner.IsRunning)
                    return runner;
            }

            return null;
        }

        private static bool HasSpawnedLocalPlayer()
        {
            if (PlayerAgent.All == null)
                return false;

            foreach (PlayerAgent player in PlayerAgent.All)
            {
                if (player != null
                    && player.Object != null
                    && player.Object.IsValid
                    && player.HasInputAuthority)
                    return true;
            }

            return false;
        }

        private static UnityEngine.Camera ResolveCamera()
        {
            if (ServiceLocator.TryGet(out ICameraService cameraService)
                && cameraService != null
                && cameraService.CurrentCamera != null)
                return cameraService.CurrentCamera;

            return UnityEngine.Camera.main;
        }
    }
}
