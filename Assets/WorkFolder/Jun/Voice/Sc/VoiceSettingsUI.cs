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
    private SliderSnap microphoneSnap;
    private SliderSnap outputSnap;

    private void Start()
    {
        _vivoxManager = VivoxManager.Instance;

        if (_vivoxManager == null)
        {
            Debug.LogError("ui가 비복스 매니저 못찾음");
            enabled = false;
            return;
        }

        microphoneVolumeSlider.minValue = 0f;
        microphoneVolumeSlider.maxValue = 75f;
        voiceOutputVolumeSlider.minValue = 0f;
        voiceOutputVolumeSlider.maxValue = 100f;
        microphoneSnap = SliderSnap.Attach(microphoneVolumeSlider, 5f);
        outputSnap = SliderSnap.Attach(voiceOutputVolumeSlider, 5f);

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

        microphoneSnap.SnapAndSet(_vivoxManager.InputDeviceVolume);

        outputSnap.SnapAndSet(_vivoxManager.OutputDeviceVolume);
    }

    private void OnMicrophoneMuteChanged(bool isMuted)
    {
        _vivoxManager.SetUserMicrophoneEnabled(!isMuted);
    }

    private void OnMicrophoneVolumeChanged(float value)
    {
        value = microphoneSnap.SnapAndSet(value);
        _vivoxManager.SetInputDeviceVolume(value);
    }

    private void OnVoiceOutputVolumeChanged(float value)
    {
        value = outputSnap.SnapAndSet(value);
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
