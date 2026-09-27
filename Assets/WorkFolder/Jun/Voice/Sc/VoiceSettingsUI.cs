using UnityEngine;
using UnityEngine.UI;

public sealed class VoiceSettingsUI : MonoBehaviour
{
    [Header("Microphone")]
    [SerializeField] private Toggle microphoneMuteToggle;
    [SerializeField] private Slider microphoneVolumeSlider;

    [Header("Voice Output")]
    [SerializeField] private Slider voiceOutputVolumeSlider;

    private VivoxManager _vivoxManager;

    private void Start()
    {
        _vivoxManager = VivoxManager.Instance;

        if (_vivoxManager == null)
        {
            Debug.LogError("ui가 비복스 매니저 못찾음");
            enabled = false;
            return;
        }

        InitializeUI();

        microphoneMuteToggle.onValueChanged
            .AddListener(OnMicrophoneMuteChanged);

        microphoneVolumeSlider.onValueChanged
            .AddListener(OnMicrophoneVolumeChanged);

        voiceOutputVolumeSlider.onValueChanged
            .AddListener(OnVoiceOutputVolumeChanged);
    }

    private void InitializeUI()
    {
        microphoneMuteToggle.SetIsOnWithoutNotify(
            !_vivoxManager.UserMicEnabled
        );

        microphoneVolumeSlider.SetValueWithoutNotify(
            _vivoxManager.InputDeviceVolume
        );

        voiceOutputVolumeSlider.SetValueWithoutNotify(
            _vivoxManager.OutputDeviceVolume
        );
    }

    private void OnMicrophoneMuteChanged(bool isMuted)
    {
        _vivoxManager.SetUserMicrophoneEnabled(!isMuted);
    }

    private void OnMicrophoneVolumeChanged(float value)
    {
        _vivoxManager.SetInputDeviceVolume(value);
    }

    private void OnVoiceOutputVolumeChanged(float value)
    {
        _vivoxManager.SetOutputDeviceVolume(value);
    }

    private void OnDestroy()
    {
        if (microphoneMuteToggle != null)
        {
            microphoneMuteToggle.onValueChanged
                .RemoveListener(OnMicrophoneMuteChanged);
        }

        if (microphoneVolumeSlider != null)
        {
            microphoneVolumeSlider.onValueChanged
                .RemoveListener(OnMicrophoneVolumeChanged);
        }

        if (voiceOutputVolumeSlider != null)
        {
            voiceOutputVolumeSlider.onValueChanged
                .RemoveListener(OnVoiceOutputVolumeChanged);
        }
    }
}