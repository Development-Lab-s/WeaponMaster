using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using WorkFolder.PAP.Scripts.SO.DataFolder;

public class AudioSettingController : MonoBehaviour
{
        [Header("Setting Data")]
    [SerializeField] private SoundSoV2 soundSoV2;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    private const string MasterParameter = "Master";
    private const string BGMParameter = "BGM";
    private const string SFXParameter = "SFX";

    private SliderSnap masterSnap;
    private SliderSnap bgmSnap;
    private SliderSnap sfxSnap;

    private void OnEnable()
    {
        masterSnap = SliderSnap.Attach(masterSlider, 0.05f);
        bgmSnap = SliderSnap.Attach(bgmSlider, 0.05f);
        sfxSnap = SliderSnap.Attach(sfxSlider, 0.05f);

        masterSlider.onValueChanged.AddListener(ChangeMasterVolume);
        bgmSlider.onValueChanged.AddListener(ChangeBGMVolume);
        sfxSlider.onValueChanged.AddListener(ChangeSFXVolume);
    }

    private void Start()
    {
        // SO 값에 맞춰 슬라이더 초기화
        soundSoV2.MasterVolume = masterSnap.SnapAndSet(soundSoV2.MasterVolume);
        soundSoV2.BGMVolume = bgmSnap.SnapAndSet(soundSoV2.BGMVolume);
        soundSoV2.SFXVolume = sfxSnap.SnapAndSet(soundSoV2.SFXVolume);

        // 게임 시작 시 SO 값을 AudioMixer에 적용
        ApplyVolume(MasterParameter, soundSoV2.MasterVolume);
        ApplyVolume(BGMParameter, soundSoV2.BGMVolume);
        ApplyVolume(SFXParameter, soundSoV2.SFXVolume);
    }

    private void OnDisable()
    {
        masterSlider.onValueChanged.RemoveListener(ChangeMasterVolume);
        bgmSlider.onValueChanged.RemoveListener(ChangeBGMVolume);
        sfxSlider.onValueChanged.RemoveListener(ChangeSFXVolume);
    }

    private void ChangeMasterVolume(float value)
    {
        value = masterSnap.SnapAndSet(value);
        soundSoV2.MasterVolume = value;
        ApplyVolume(MasterParameter, value);
    }

    private void ChangeBGMVolume(float value)
    {
        value = bgmSnap.SnapAndSet(value);
        soundSoV2.BGMVolume = value;
        ApplyVolume(BGMParameter, value);
    }

    private void ChangeSFXVolume(float value)
    {
        value = sfxSnap.SnapAndSet(value);
        soundSoV2.SFXVolume = value;
        ApplyVolume(SFXParameter, value);
    }

    private void ApplyVolume(string parameterName, float value)
    {
        // 슬라이더의 0~1 값을 AudioMixer의 데시벨 값으로 변환
        float decibel = value <= 0.0001f
            ? -80f
            : Mathf.Log10(value) * 20f;

        audioMixer.SetFloat(parameterName, decibel);
    }
}
