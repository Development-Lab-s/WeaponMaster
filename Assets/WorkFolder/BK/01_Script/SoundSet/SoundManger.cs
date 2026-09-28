using System;
using BK._01_Script.SO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SoundManger : MonoBehaviour
{
    [FormerlySerializedAs("mouseSettings")]
    [Header("사운드 데이터")]
    [SerializeField] private SoundSO soundSettings;

    [Header("슬라이더")]
    [SerializeField] private UnityEngine.UI.Slider masterSlider;
    [SerializeField] private UnityEngine.UI.Slider bgmSlider;
    [SerializeField] private UnityEngine.UI.Slider vfxSlider;

    [Header("오디오 믹서")]
    //[SerializeField] private AudioMixer audioMixer;

    private const string MasterParameter = "MasterVolume";
    private const string BgmParameter = "BgmVolume";
    private const string VfxParameter = "VfxVolume";

    private SliderSnap masterSnap;
    private SliderSnap bgmSnap;
    private SliderSnap vfxSnap;

    private void Awake()
    {
        if (!CheckReferences())
        {
            enabled = false;
            return;
        }

        ConfigureSlider(masterSlider);
        ConfigureSlider(bgmSlider);
        ConfigureSlider(vfxSlider);

        masterSnap = SliderSnap.Attach(masterSlider, 0.05f);
        bgmSnap = SliderSnap.Attach(bgmSlider, 0.05f);
        vfxSnap = SliderSnap.Attach(vfxSlider, 0.05f);

        masterSlider.onValueChanged.AddListener(OnMasterChanged);
        bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        vfxSlider.onValueChanged.AddListener(OnVfxChanged);

        ApplySOValuesToSliders();
    }

    private void Start()
    {
        ApplyAllMixerVolumes();
    }

    private void OnEnable()
    {
        // 설정창을 다시 열었을 때 최신 SO 값으로 동기화
        if (soundSettings != null)
        {
            ApplySOValuesToSliders();
        }
    }

    private void OnDestroy()
    {
        if (masterSlider != null)
        {
            masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
        }

        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveListener(OnBgmChanged);
        }

        if (vfxSlider != null)
        {
            vfxSlider.onValueChanged.RemoveListener(OnVfxChanged);
        }
    }

    private void ConfigureSlider(UnityEngine.UI.Slider slider)
    {
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
    }

    private void ApplySOValuesToSliders()
    {
        if (soundSettings == null)
        {
            return;
        }

        masterSnap.SnapAndSet(soundSettings.MasterVolume);
        bgmSnap.SnapAndSet(soundSettings.BgmVolume);
        vfxSnap.SnapAndSet(soundSettings.VfxVolume);
    }

    private void OnMasterChanged(float value)
    {
        value = masterSnap.SnapAndSet(value);
        soundSettings.MasterVolume = value;
        //SetMixerVolume(MasterParameter, value);
    }

    private void OnBgmChanged(float value)
    {
        value = bgmSnap.SnapAndSet(value);
        soundSettings.BgmVolume = value;
        //SetMixerVolume(BgmParameter, value);
    }

    private void OnVfxChanged(float value)
    {
        value = vfxSnap.SnapAndSet(value);
        soundSettings.VfxVolume = value;
        //SetMixerVolume(VfxParameter, value);
    }

    private void ApplyAllMixerVolumes()
    {
        /*SetMixerVolume(
            MasterParameter,
            soundSettings.MasterVolume
        );

        SetMixerVolume(
            BgmParameter,
            soundSettings.BgmVolume
        );

        SetMixerVolume(
            VfxParameter,
            soundSettings.VfxVolume
        );*/
    }

    /*private void SetMixerVolume(string parameterName, float value)
    {
        // 슬라이더의 0~1 값을 AudioMixer의 데시벨로 변환
        float decibel = value <= 0.0001f
            ? -80f
            : Mathf.Log10(value) * 20f;

        bool success = audioMixer.SetFloat(
            parameterName,
            decibel
        );

        if (!success)
        {
            Debug.LogError(
                $"AudioMixer 매개변수를 찾지 못했습니다: {parameterName}"
            );
        }
    }*/

    private bool CheckReferences()
    {
        if (soundSettings == null)
        {
            Debug.LogError("SoundSettingsSO가 연결되지 않았습니다.");
            return false;
        }

        if (masterSlider == null ||
            bgmSlider == null ||
            vfxSlider == null)
        {
            Debug.LogError("사운드 슬라이더가 연결되지 않았습니다.");
            return false;
        }

        /*if (audioMixer == null)
        {
            Debug.LogError("AudioMixer가 연결되지 않았습니다.");
            return false;
        }*/

        return true;
    }
}
