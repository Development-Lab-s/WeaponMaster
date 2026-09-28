using TMPro;
using UnityEngine;
using WorkFolder.PAP.Scripts.SO.DataFolder;

public class SensitivitySliderPanelUI : MonoBehaviour
{
    [Header("변경할 설정")]
    [SerializeField] private SettingValueSO settingSO;

    [Header("UI")]
    [SerializeField] private UnityEngine.UI.Slider cameraSensitivitySlider;
    [SerializeField] private UnityEngine.UI.Slider weaponSensitivitySlider;
    [SerializeField] private UnityEngine.UI.Toggle invertYToggle;
    [SerializeField] private TMP_Text cameraValueText;
    [SerializeField] private TMP_Text weaponValueText;

    private void OnEnable()
    {
        if (!IsReady())
            return;

        cameraSensitivitySlider.onValueChanged.AddListener(SetCameraSensitivity);
        weaponSensitivitySlider.onValueChanged.AddListener(SetWeaponSensitivity);
        invertYToggle.onValueChanged.AddListener(SetInvertY);

        RefreshFromSettings();
    }

    private void OnDisable()
    {
        if (cameraSensitivitySlider != null)
            cameraSensitivitySlider.onValueChanged.RemoveListener(SetCameraSensitivity);

        if (weaponSensitivitySlider != null)
            weaponSensitivitySlider.onValueChanged.RemoveListener(SetWeaponSensitivity);

        if (invertYToggle != null)
            invertYToggle.onValueChanged.RemoveListener(SetInvertY);
    }

    public void RefreshFromSettings()
    {
        if (!IsReady())
            return;

        float cameraValue = Mathf.Clamp(
            (Mathf.Abs(settingSO.MouseSensitivityX) + Mathf.Abs(settingSO.MouseSensitivityY)) * 0.5f,
            cameraSensitivitySlider.minValue,
            cameraSensitivitySlider.maxValue);
        float weaponValue = Mathf.Clamp(
            (Mathf.Abs(settingSO.WeaponSensitivityX) + Mathf.Abs(settingSO.WeaponSensitivityY)) * 0.5f,
            weaponSensitivitySlider.minValue,
            weaponSensitivitySlider.maxValue);

        cameraSensitivitySlider.SetValueWithoutNotify(cameraValue);
        weaponSensitivitySlider.SetValueWithoutNotify(weaponValue);
        invertYToggle.SetIsOnWithoutNotify(settingSO.MouseSensitivityY < 0f);

        UpdateValueText(cameraValueText, cameraValue);
        UpdateValueText(weaponValueText, weaponValue);
    }

    private void SetCameraSensitivity(float value)
    {
        value = RoundSensitivity(value);
        settingSO.MouseSensitivityX = value;
        settingSO.MouseSensitivityY = invertYToggle.isOn ? -value : value;
        UpdateValueText(cameraValueText, value);
    }

    private void SetWeaponSensitivity(float value)
    {
        value = RoundSensitivity(value);
        settingSO.WeaponSensitivityX = value;
        settingSO.WeaponSensitivityY = value;
        UpdateValueText(weaponValueText, value);
    }

    private void SetInvertY(bool invert)
    {
        float value = Mathf.Abs(settingSO.MouseSensitivityY);
        settingSO.MouseSensitivityY = invert ? -value : value;
    }

    private bool IsReady()
    {
        return settingSO != null
            && cameraSensitivitySlider != null
            && weaponSensitivitySlider != null
            && invertYToggle != null;
    }

    private static float RoundSensitivity(float value)
    {
        return Mathf.Round(value * 100f) / 100f;
    }

    private static void UpdateValueText(TMP_Text target, float value)
    {
        if (target != null)
            target.text = value.ToString("F2");
    }
}
