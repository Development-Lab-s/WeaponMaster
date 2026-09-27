using UnityEngine;
using UnityEngine.UI;

public sealed class VoiceSettingsUI : MonoBehaviour
{
    [Header("Microphone")]
    [SerializeField]
    private Button microphoneMuteButton;

    [SerializeField]
    private Slider microphoneVolumeSlider;

    [Header("Voice Output")]
    [SerializeField]
    private Slider voiceOutputVolumeSlider;


    private VivoxManager _vivoxManager;


    private void Start()
    {
        _vivoxManager = VivoxManager.Instance;

        if (_vivoxManager == null)
        {
            Debug.LogError(
                "VoiceSettingsUI: VivoxManager not found."
            );

            enabled = false;
            return;
        }

        InitializeUI();

        microphoneMuteButton.onClick
            .AddListener(OnMicrophoneMuteClicked);

        microphoneVolumeSlider.onValueChanged
            .AddListener(OnMicrophoneVolumeChanged);

        voiceOutputVolumeSlider.onValueChanged
            .AddListener(OnVoiceOutputVolumeChanged);
    }


    private void InitializeUI()
    {
        // 이벤트를 발생시키지 않고 현재 설정값만 표시.
        microphoneVolumeSlider.SetValueWithoutNotify(
            _vivoxManager.InputDeviceVolume
        );

        voiceOutputVolumeSlider.SetValueWithoutNotify(
            _vivoxManager.OutputDeviceVolume
        );
    }


    private void OnMicrophoneMuteClicked()
    {
        _vivoxManager.ToggleUserMicrophone();
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
        if (microphoneMuteButton != null)
        {
            microphoneMuteButton.onClick
                .RemoveListener(OnMicrophoneMuteClicked);
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