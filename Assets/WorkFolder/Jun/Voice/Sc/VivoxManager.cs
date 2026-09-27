using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;

public sealed class VivoxManager : MonoBehaviour
{
    public static VivoxManager Instance { get; private set; }

    // 나중에 UI 표시 등이 필요할 때 사용할 수 있음.
    // true = 실제 Vivox 마이크가 음소거된 상태.
    public event Action<bool> MicMuteStateChanged;

    [Header("3D Voice")]
    [SerializeField] private int audibleDistance = 30;
    [SerializeField] private int conversationalDistance = 2;
    [SerializeField] private float fadeIntensity = 2.1f;

    [Header("Position Update")]
    [SerializeField] private float updateInterval = 0.3f;
    [SerializeField] private float minMoveDistance = 0.05f;
    [SerializeField] private float minRotationAngle = 1f;

    [Header("Voice Settings")]
    [SerializeField] private bool microphoneEnabledByDefault = true;

    // Vivox의 기본 볼륨은 50.
    [SerializeField, Range(0, 75)]
    private int inputDeviceVolume = 50;

    [SerializeField, Range(0, 100)]
    private int outputDeviceVolume = 50;


    private Task _readyTask;

    private string _currentChannel;
    private GameObject _positionTarget;

    private bool _vivoxReady;
    private bool _isChannelJoined;
    private bool _hasSentPosition;

    // 사용자 설정.
    private bool _userMicEnabled;

    // 게임 상태.
    // Player가 Spawn되기 전에는 말할 수 없게 false로 시작.
    private bool _isPlayerAlive;

    // 같은 Mute 상태를 Vivox에 계속 재적용하지 않기 위한 값.
    private bool? _lastAppliedMuteState;

    private float _nextUpdateTime;

    private Vector3 _lastPosition;
    private Quaternion _lastRotation;


    public bool UserMicEnabled => _userMicEnabled;
    public bool IsPlayerAlive => _isPlayerAlive;

    public bool IsMicMuted =>
        !_userMicEnabled || !_isPlayerAlive;

    public int InputDeviceVolume => inputDeviceVolume;
    public int OutputDeviceVolume => outputDeviceVolume;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        _userMicEnabled = microphoneEnabledByDefault;
        _isPlayerAlive = false;

        _readyTask = InitializeVivoxAsync();
    }


    private async Task InitializeVivoxAsync()
    {
        try
        {
            if (UnityServices.State ==
                ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }
            else
            {
                while (UnityServices.State ==
                       ServicesInitializationState.Initializing)
                {
                    await Task.Yield();
                }
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance
                    .SignInAnonymouslyAsync();
            }

            await VivoxService.Instance.InitializeAsync();
            await VivoxService.Instance.LoginAsync();

            _vivoxReady = true;

            ApplyAudioSettings();

            Debug.Log("Vivox Login Success");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            throw;
        }
    }


    // -------------------------------------------------------
    // Fusion Room / Proximity Voice
    // -------------------------------------------------------

    public async Task BindToFusionRoomAsync(
        string fusionRoomName,
        GameObject localPlayer)
    {
        if (string.IsNullOrWhiteSpace(fusionRoomName))
        {
            Debug.LogError("Fusion room name is empty.");
            return;
        }

        if (localPlayer == null)
        {
            Debug.LogError("Local player is null.");
            return;
        }

        _positionTarget = localPlayer;

        await _readyTask;

        // 서버에서 방 이름의 유일성과 유효한 문자를 보장한다고 했으므로
        // 별도 문자 필터링은 하지 않는다.
        string targetChannel = "prox_" + fusionRoomName;

        // 이미 같은 방에 있다면 위치 Target만 갱신.
        if (_isChannelJoined &&
            _currentChannel == targetChannel)
        {
            _hasSentPosition = false;
            return;
        }

        // 다른 Vivox 방에 있었다면 먼저 퇴장.
        if (_isChannelJoined)
        {
            string previousChannel = _currentChannel;

            // Leave 중 Update에서 위치를 보내지 않도록 먼저 false.
            _isChannelJoined = false;
            _currentChannel = null;

            await VivoxService.Instance
                .LeaveChannelAsync(previousChannel);
        }

        Channel3DProperties properties =
            new Channel3DProperties(
                audibleDistance,
                conversationalDistance,
                fadeIntensity,
                AudioFadeModel.InverseByDistance
            );

        await VivoxService.Instance
            .JoinPositionalChannelAsync(
                targetChannel,
                ChatCapability.AudioOnly,
                properties
            );

        _currentChannel = targetChannel;
        _isChannelJoined = true;

        _hasSentPosition = false;
        _nextUpdateTime = Time.unscaledTime;

        Debug.Log(
            $"Vivox Positional Joined: {_currentChannel}");
    }


    public void UnbindLocalPlayer(GameObject player)
    {
        if (_positionTarget == player)
        {
            _positionTarget = null;
            _hasSentPosition = false;
        }
    }


    public async Task LeaveCurrentChannelAsync()
    {
        await _readyTask;

        if (!_isChannelJoined)
            return;

        string channelToLeave = _currentChannel;

        _isChannelJoined = false;
        _currentChannel = null;
        _positionTarget = null;
        _hasSentPosition = false;

        await VivoxService.Instance
            .LeaveChannelAsync(channelToLeave);
    }


    // -------------------------------------------------------
    // Microphone State
    // -------------------------------------------------------

    public void ToggleUserMicrophone()
    {
        SetUserMicrophoneEnabled(!_userMicEnabled);
    }


    public void SetUserMicrophoneEnabled(bool enabled)
    {
        if (_userMicEnabled == enabled)
            return;

        _userMicEnabled = enabled;

        RefreshMicrophoneMuteState();
    }


    public void SetPlayerAlive(bool alive)
    {
        if (_isPlayerAlive == alive)
            return;

        _isPlayerAlive = alive;

        RefreshMicrophoneMuteState();
    }


    private void RefreshMicrophoneMuteState(
        bool force = false)
    {
        if (!_vivoxReady)
            return;

        bool shouldMute = IsMicMuted;

        if (!force &&
            _lastAppliedMuteState.HasValue &&
            _lastAppliedMuteState.Value == shouldMute)
        {
            return;
        }

        if (shouldMute)
        {
            VivoxService.Instance.MuteInputDevice();
        }
        else
        {
            VivoxService.Instance.UnmuteInputDevice();
        }

        _lastAppliedMuteState = shouldMute;

        MicMuteStateChanged?.Invoke(shouldMute);
    }


    // -------------------------------------------------------
    // Volume
    // -------------------------------------------------------

    public void SetInputDeviceVolume(float value)
    {
        // 마이크 입력은 75 초과를 막는다.
        int volume = Mathf.Clamp(
            Mathf.RoundToInt(value),
            0,
            75
        );

        if (inputDeviceVolume == volume)
            return;

        inputDeviceVolume = volume;

        if (_vivoxReady)
        {
            VivoxService.Instance
                .SetInputDeviceVolume(inputDeviceVolume);
        }
    }


    public void SetOutputDeviceVolume(float value)
    {
        int volume = Mathf.Clamp(
            Mathf.RoundToInt(value),
            0,
            100
        );

        if (outputDeviceVolume == volume)
            return;

        outputDeviceVolume = volume;

        if (_vivoxReady)
        {
            VivoxService.Instance
                .SetOutputDeviceVolume(outputDeviceVolume);
        }
    }


    private void ApplyAudioSettings()
    {
        if (!_vivoxReady)
            return;

        VivoxService.Instance
            .SetInputDeviceVolume(inputDeviceVolume);

        VivoxService.Instance
            .SetOutputDeviceVolume(outputDeviceVolume);

        RefreshMicrophoneMuteState(true);
    }


    // -------------------------------------------------------
    // Positional Update
    // -------------------------------------------------------

    private void Update()
    {
        if (!_isChannelJoined ||
            _positionTarget == null)
        {
            return;
        }

        if (Time.unscaledTime < _nextUpdateTime)
            return;

        _nextUpdateTime =
            Time.unscaledTime + updateInterval;

        Transform targetTransform =
            _positionTarget.transform;

        Vector3 currentPosition =
            targetTransform.position;

        Quaternion currentRotation =
            targetTransform.rotation;

        float moveThresholdSqr =
            minMoveDistance * minMoveDistance;

        bool moved =
            !_hasSentPosition ||
            (currentPosition - _lastPosition)
            .sqrMagnitude >= moveThresholdSqr;

        bool rotated =
            !_hasSentPosition ||
            Quaternion.Angle(
                currentRotation,
                _lastRotation
            ) >= minRotationAngle;

        if (!moved && !rotated)
            return;

        VivoxService.Instance.Set3DPosition(
            _positionTarget,
            _currentChannel
        );

        _lastPosition = currentPosition;
        _lastRotation = currentRotation;
        _hasSentPosition = true;
    }
}