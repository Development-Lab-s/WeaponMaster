using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Vivox;

public sealed class VivoxManager : MonoBehaviour
{
    public static VivoxManager Instance { get; private set; }

    [Header("3D Voice")]
    [SerializeField] private int audibleDistance = 15;
    [SerializeField] private int conversationalDistance = 2;
    [SerializeField] private float fadeIntensity = 4.3f;

    [Header("Position Update")]
    [SerializeField] private float updateInterval = 0.3f;
    [SerializeField] private float minMoveDistance = 0.05f;
    [SerializeField] private float minRotationAngle = 1f;

    private Task _readyTask;

    private string _currentChannel;
    private GameObject _positionTarget;

    private bool _isChannelJoined;
    private bool _hasSentPosition;

    private float _nextUpdateTime;

    private Vector3 _lastPosition;
    private Quaternion _lastRotation;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _readyTask = InitializeVivoxAsync();
    }

    private async Task InitializeVivoxAsync()
    {
        await WorkFolder.SUH.Scripts.DataStoreService.Lib.UgsSignIn.EnsureSignedInAsync();

        await VivoxService.Instance.InitializeAsync();
        await VivoxService.Instance.LoginAsync();

        Debug.Log("Vivox Login Success");
    }

    public async Task BindToFusionRoomAsync(
        string fusionRoomName,
        GameObject localPlayer)
    {
        _positionTarget = localPlayer;

        await _readyTask;

        // 서버에서 이미 유효하고 유일한 이름을 보장하므로
        // 따로 문자열 정리할 필요 없음.
        string targetChannel = "prox_" + fusionRoomName;

        if (_isChannelJoined && _currentChannel == targetChannel)
        {
            _hasSentPosition = false;
            return;
        }

        if (_isChannelJoined)
        {
            await VivoxService.Instance.LeaveChannelAsync(_currentChannel);
            _isChannelJoined = false;
        }

        Channel3DProperties properties = new Channel3DProperties(
            audibleDistance,
            conversationalDistance,
            fadeIntensity,
            AudioFadeModel.InverseByDistance
        );

        await VivoxService.Instance.JoinPositionalChannelAsync(
            targetChannel,
            ChatCapability.AudioOnly,
            properties
        );

        _currentChannel = targetChannel;
        _isChannelJoined = true;

        _hasSentPosition = false;
        _nextUpdateTime = Time.unscaledTime;

        Debug.Log($"Vivox Positional Joined: {_currentChannel}");
    }

    public void UnbindLocalPlayer(GameObject player)
    {
        if (_positionTarget == player)
        {
            _positionTarget = null;
        }
    }

    private void Update()
    {
        if (!_isChannelJoined || _positionTarget == null)
            return;

        if (Time.unscaledTime < _nextUpdateTime)
            return;

        _nextUpdateTime = Time.unscaledTime + updateInterval;

        Transform target = _positionTarget.transform;

        Vector3 currentPosition = target.position;
        Quaternion currentRotation = target.rotation;

        float moveThresholdSqr = minMoveDistance * minMoveDistance;

        bool moved =
            !_hasSentPosition ||
            (currentPosition - _lastPosition).sqrMagnitude >= moveThresholdSqr;

        bool rotated =
            !_hasSentPosition ||
            Quaternion.Angle(currentRotation, _lastRotation) >= minRotationAngle;

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