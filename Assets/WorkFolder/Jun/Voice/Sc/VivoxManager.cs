using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Vivox;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;

public sealed class VivoxManager : MonoBehaviour
{
    public static VivoxManager Instance { get; private set; }

    public event Action<bool> MicMuteStateChanged;
    public event Action<bool> LocalSpeechDetectedChanged;

    [Header("3D Voice")]
    [SerializeField] private int audibleDistance = 90;
    // 이 거리 안은 원래 크기. 감쇠 세기 1이면 10 m에서 약 -4 dB, 20 m에서 약 -10 dB.
    [SerializeField] private int conversationalDistance = 6;
    [SerializeField] private float fadeIntensity = 1f;

    [Header("Position Update")]
    [SerializeField] private float updateInterval = 0.3f;
    [SerializeField] private float minMoveDistance = 0.05f;
    [SerializeField] private float minRotationAngle = 1f;

    [Header("Voice Settings")]
    [SerializeField] private bool microphoneEnabledByDefault = true;

    // 화면에 보이는 볼륨(0~100)이다. 50이 장치 원래 크기이고, 0은 거의 무음, 100은 최대 증폭.
    // Vivox에는 이 값을 그대로 넘기지 않고 dB로 바꿔 넘긴다(ToVivoxVolume).
    [SerializeField, Range(0, MaximumVolume)]
    private int inputDeviceVolume = NeutralVolume;

    [SerializeField, Range(0, MaximumVolume)]
    private int outputDeviceVolume = NeutralVolume;

    // 볼륨 100일 때 올릴 최대 증폭(dB). Vivox 값은 dB 그대로라 50을 넘기면 +50 dB(약 316배)가
    // 되어 목소리가 깨지고 배경 잡음까지 같이 커진다. 마이크는 잡음이 함께 커지므로 더 낮게 둔다.
    [SerializeField, Range(0, 20)] private int maximumInputBoostDb = 6;
    [SerializeField, Range(0, 20)] private int maximumOutputBoostDb = 10;

    [Header("Voice Processing")]
    [SerializeField] private bool noiseSuppression = true;
    [SerializeField] private bool echoCancellation = true;
    [SerializeField] private bool automaticGainControl = true;
    [SerializeField] private bool clippingProtection = true;

    private const int NeutralVolume = 50;
    private const int MaximumVolume = 100;
    private const int VivoxMinimumVolume = -50;


    private Task _readyTask;

    private const int MaxLoginAttempts = 5;
    private static readonly TimeSpan LoginRetryDelay = TimeSpan.FromSeconds(10);

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
            // 다른 UGS 기능과 로그인 작업을 공유해 동시 로그인 충돌을 피합니다.
            await UgsSignIn.EnsureSignedInAsync();

            await VivoxService.Instance.InitializeAsync();

            ApplyVoiceProcessing();

            BindParticipantEvents();

            await LoginWithRetryAsync();

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

    private async Task LoginWithRetryAsync()
    {
        for (int attempt = 1; attempt <= MaxLoginAttempts; attempt++)
        {
            try
            {
                await VivoxService.Instance.LoginAsync();
                return;
            }
            catch (VivoxApiException exception)
                when (exception.StatusCode == 5043 && attempt < MaxLoginAttempts)
            {
                Debug.LogWarning(
                    $"Vivox 로그인 시간 초과 ({attempt}/{MaxLoginAttempts}). " +
                    $"{LoginRetryDelay.TotalSeconds:0}초 후 재시도합니다.");
                await Task.Delay(LoginRetryDelay);
            }
        }
    }

    private Task EnsureReadyAsync()
    {
        // 이전 매치에서 모든 재시도가 실패했다면 새 매치에서 다시 시도합니다.
        if (_readyTask == null || _readyTask.IsFaulted || _readyTask.IsCanceled)
            _readyTask = InitializeVivoxAsync();

        return _readyTask;
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

        await EnsureReadyAsync();

        string targetChannel =
            ToChannelName(fusionRoomName);

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


    // Vivox 채널 이름은 영문, 숫자, =+-_.!~()% 만 쓸 수 있다. 방 이름은 로비에서 직접 입력해
    // 한글이나 공백이 들어가면 채널 입장이 예외로 실패해 그 방에서는 음성이 전혀 안 된다.
    // 방 이름을 해시해 영문·숫자로 바꾼다. 같은 방이면 모든 클라이언트가 같은 이름을 얻는다.
    private static string ToChannelName(string fusionRoomName)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(fusionRoomName));
            return "prox_" + BitConverter.ToString(hash, 0, 16).Replace("-", "");
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
            MaximumVolume
        );

        if (inputDeviceVolume == volume)
            return;

        inputDeviceVolume = volume;

        if (_vivoxReady)
        {
            VivoxService.Instance
                .SetInputDeviceVolume(
                    ToVivoxVolume(inputDeviceVolume, maximumInputBoostDb)
                );
        }
    }


    public void SetOutputDeviceVolume(float value)
    {
        int volume = Mathf.Clamp(
            Mathf.RoundToInt(value),
            0,
            MaximumVolume
        );

        if (outputDeviceVolume == volume)
            return;

        outputDeviceVolume = volume;

        if (_vivoxReady)
        {
            VivoxService.Instance
                .SetOutputDeviceVolume(
                    ToVivoxVolume(outputDeviceVolume, maximumOutputBoostDb)
                );
        }
    }


    private void ApplyAudioSettings()
    {
        if (!_vivoxReady)
            return;

        VivoxService.Instance
            .SetInputDeviceVolume(
                ToVivoxVolume(inputDeviceVolume, maximumInputBoostDb)
            );

        VivoxService.Instance
            .SetOutputDeviceVolume(
                ToVivoxVolume(outputDeviceVolume, maximumOutputBoostDb)
            );

        RefreshMicrophoneMuteState(true);
    }


    // Vivox 장치 볼륨은 -50~+50의 dB 값이고 0이 원래 크기다. 화면 볼륨 50을 0 dB에 맞추고,
    // 그 아래는 -50 dB(거의 무음)까지 줄이며, 그 위는 정해 둔 최대 증폭까지만 올린다.
    private static int ToVivoxVolume(int volume, int maximumBoostDb)
    {
        if (volume <= NeutralVolume)
        {
            return Mathf.RoundToInt(Mathf.Lerp(
                VivoxMinimumVolume,
                0f,
                volume / (float)NeutralVolume));
        }

        return Mathf.RoundToInt(Mathf.Lerp(
            0f,
            maximumBoostDb,
            (volume - NeutralVolume) / (float)(MaximumVolume - NeutralVolume)));
    }


    // 잡음 제거, 에코 제거, 자동 음량 조절, 클리핑 보호를 명시적으로 켠다.
    // Vivox가 초기화된 뒤에만 설정할 수 있다.
    private void ApplyVoiceProcessing()
    {
        try
        {
            IVivoxGlobalAudioSettings settings =
                VivoxService.Instance.VivoxGlobalAudioSettings;

            settings.NoiseSuppressionEnabled = noiseSuppression;
            settings.VivoxAcousticEchoCancellationEnabled = echoCancellation;
            settings.AutomaticGainControlEnabled = automaticGainControl;
            settings.AudioClippingProtectorEnabled = clippingProtection;
            // 켜면 3D 채널 전체가 6 dB 작아져, 거리 감쇠와 겹쳐 조금만 떨어져도 안 들린다.
            // 증폭을 몇 dB로 제한했고 클리핑 보호가 켜져 있으니 끈다.
            settings.PositionalChannelVolumeProtectionEnabled = false;
        }
        catch (NotImplementedException)
        {
            // WebGL은 전역 음성 처리 설정을 지원하지 않는다.
        }
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
