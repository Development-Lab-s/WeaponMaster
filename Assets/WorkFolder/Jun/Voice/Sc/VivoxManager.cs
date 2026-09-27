using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;

public sealed class VivoxManager : MonoBehaviour
{
    public static VivoxManager Instance { get; private set; }

    public event Action<bool> MicMuteStateChanged;
    public event Action<bool> LocalSpeechDetectedChanged;

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

    [SerializeField, Range(0, 75)]
    private int inputDeviceVolume = 50;

    [SerializeField, Range(0, 100)]
    private int outputDeviceVolume = 50;


    private Task _readyTask;

    private string _currentChannel;
    private string _joiningChannel;

    private GameObject _positionTarget;

    private bool _vivoxReady;
    private bool _isChannelJoined;
    private bool _hasSentPosition;

    private bool _userMicEnabled;
    private bool _isPlayerAlive;

    private bool? _lastAppliedMuteState;

    private float _nextUpdateTime;

    private Vector3 _lastPosition;
    private Quaternion _lastRotation;


    // -----------------------------
    // Speaking Detection
    // -----------------------------

    private VivoxParticipant _localParticipant;

    private bool _participantEventsBound;
    private bool _isLocalSpeaking;


    // -----------------------------
    // Public Properties
    // -----------------------------

    public bool UserMicEnabled => _userMicEnabled;

    public bool IsPlayerAlive => _isPlayerAlive;

    public bool IsMicMuted =>
        !_userMicEnabled || !_isPlayerAlive;

    public bool IsLocalSpeaking => _isLocalSpeaking;

    public int InputDeviceVolume => inputDeviceVolume;

    public int OutputDeviceVolume => outputDeviceVolume;


    // -----------------------------
    // Unity
    // -----------------------------

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


    private void OnDestroy()
    {
        if (Instance != this)
            return;

        UnbindParticipantEvents();
        DetachLocalParticipant();

        Instance = null;
    }


    // -----------------------------
    // Vivox Initialization
    // -----------------------------

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

            BindParticipantEvents();

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


    // -----------------------------
    // Participant Events
    // -----------------------------

    private void BindParticipantEvents()
    {
        if (_participantEventsBound)
            return;

        VivoxService.Instance.ParticipantAddedToChannel
            += OnParticipantAdded;

        VivoxService.Instance.ParticipantRemovedFromChannel
            += OnParticipantRemoved;

        _participantEventsBound = true;
    }


    private void UnbindParticipantEvents()
    {
        if (!_participantEventsBound)
            return;

        VivoxService.Instance.ParticipantAddedToChannel
            -= OnParticipantAdded;

        VivoxService.Instance.ParticipantRemovedFromChannel
            -= OnParticipantRemoved;

        _participantEventsBound = false;
    }


    private void OnParticipantAdded(VivoxParticipant participant)
    {
        // 머리 위 아이콘용으로 필요한 건
        // 다른 사람이 아니라 이 클라이언트 자신의 발화 상태.
        if (!participant.IsSelf)
            return;

        bool expectedChannel =
            participant.ChannelName == _currentChannel ||
            participant.ChannelName == _joiningChannel;

        if (!expectedChannel)
            return;

        DetachLocalParticipant();

        _localParticipant = participant;

        _localParticipant.ParticipantSpeechDetected
            += OnLocalParticipantSpeechDetected;

        RefreshLocalSpeechState();
    }


    private void OnParticipantRemoved(VivoxParticipant participant)
    {
        if (!participant.IsSelf)
            return;

        if (_localParticipant == null)
            return;

        if (_localParticipant.PlayerId != participant.PlayerId ||
            _localParticipant.ChannelName != participant.ChannelName)
        {
            return;
        }

        DetachLocalParticipant();

        SetLocalSpeaking(false);
    }


    private void OnLocalParticipantSpeechDetected()
    {
        RefreshLocalSpeechState();
    }


    private void DetachLocalParticipant()
    {
        if (_localParticipant == null)
            return;

        _localParticipant.ParticipantSpeechDetected
            -= OnLocalParticipantSpeechDetected;

        _localParticipant = null;
    }


    private void RefreshLocalSpeechState()
    {
        bool speaking =
            _isChannelJoined &&
            !IsMicMuted &&
            _localParticipant != null &&
            _localParticipant.SpeechDetected;

        SetLocalSpeaking(speaking);
    }


    private void SetLocalSpeaking(bool speaking)
    {
        if (_isLocalSpeaking == speaking)
            return;

        _isLocalSpeaking = speaking;

        LocalSpeechDetectedChanged?.Invoke(speaking);
    }


    // -----------------------------
    // Fusion Room / Voice Channel
    // -----------------------------

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

        string targetChannel =
            "prox_" + fusionRoomName;

        // 이미 같은 방에 있다면 위치 Target만 교체.
        if (_isChannelJoined &&
            _currentChannel == targetChannel)
        {
            _hasSentPosition = false;
            RefreshLocalSpeechState();

            return;
        }

        // 기존 채널 퇴장.
        if (_isChannelJoined)
        {
            string previousChannel =
                _currentChannel;

            _isChannelJoined = false;
            _currentChannel = null;

            SetLocalSpeaking(false);

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

        _joiningChannel = targetChannel;

        try
        {
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

            RefreshLocalSpeechState();

            Debug.Log(
                $"Vivox Positional Joined: {_currentChannel}"
            );
        }
        finally
        {
            _joiningChannel = null;
        }
    }


    public void UnbindLocalPlayer(GameObject player)
    {
        if (_positionTarget != player)
            return;

        _positionTarget = null;
        _hasSentPosition = false;
    }


    public async Task LeaveCurrentChannelAsync()
    {
        await _readyTask;

        if (!_isChannelJoined)
            return;

        string channelToLeave =
            _currentChannel;

        _isChannelJoined = false;
        _currentChannel = null;

        _positionTarget = null;
        _hasSentPosition = false;

        SetLocalSpeaking(false);

        await VivoxService.Instance
            .LeaveChannelAsync(channelToLeave);
    }


    // -----------------------------
    // Microphone
    // -----------------------------

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
        {
            RefreshLocalSpeechState();
            return;
        }

        bool shouldMute = IsMicMuted;

        if (!force &&
            _lastAppliedMuteState.HasValue &&
            _lastAppliedMuteState.Value == shouldMute)
        {
            RefreshLocalSpeechState();
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

        RefreshLocalSpeechState();
    }


    // -----------------------------
    // Volume
    // -----------------------------

    public void SetInputDeviceVolume(float value)
    {
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
                .SetInputDeviceVolume(
                    inputDeviceVolume
                );
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
                .SetOutputDeviceVolume(
                    outputDeviceVolume
                );
        }
    }


    private void ApplyAudioSettings()
    {
        if (!_vivoxReady)
            return;

        VivoxService.Instance
            .SetInputDeviceVolume(
                inputDeviceVolume
            );

        VivoxService.Instance
            .SetOutputDeviceVolume(
                outputDeviceVolume
            );

        RefreshMicrophoneMuteState(true);
    }


    // -----------------------------
    // Positional Voice
    // -----------------------------

    private void Update()
    {
        if (!_isChannelJoined ||
            _positionTarget == null)
        {
            return;
        }

        if (Time.unscaledTime <
            _nextUpdateTime)
        {
            return;
        }

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