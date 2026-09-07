using System;
using UnityEngine;
using UnityEngine.UI;
using WorkFolder.PAP.Scripts.SO.DataFolder;

public class MouseGamdoSet : MonoBehaviour
{
    public enum TargetValue
    {
        MouseSensitivityX,
        MouseSensitivityY,
        WeaponSensitivityX,
        WeaponSensitivityY
    }
    private Slider slider;
    [SerializeField] private SettingValueSO sensitivitySO;
    [SerializeField] private TargetValue targetValue;

    private void Awake()
    {
        // Inspector에서 안 넣었으면 같은 오브젝트에서 가져오기
        if (slider == null)
            slider = GetComponent<Slider>();

        if (sensitivitySO == null)
        {
            Debug.LogError($"{name}: SensitivitySO가 연결되지 않았습니다.");
            return;
        }

        // 이벤트 등록
        slider.onValueChanged.AddListener(OnSliderValueChanged);

        // SO 값으로 슬라이더 초기화
        slider.SetValueWithoutNotify(GetSOValue());
    }

    private void OnDestroy()
    {
        if (slider != null)
            slider.onValueChanged.RemoveListener(OnSliderValueChanged);
    }

    private void OnSliderValueChanged(float value)
    {
        float roundedValue = Mathf.Round(value * 100f) / 100f;

        switch (targetValue)
        {
            case TargetValue.MouseSensitivityX:
                sensitivitySO.MouseSensitivityX = roundedValue;
                break;

            case TargetValue.MouseSensitivityY:
                sensitivitySO.MouseSensitivityY = roundedValue;
                break;

            case TargetValue.WeaponSensitivityX:
                sensitivitySO.WeaponSensitivityX = roundedValue;
                break;

            case TargetValue.WeaponSensitivityY:
                sensitivitySO.WeaponSensitivityY = roundedValue;
                break;
        }
    }

    private float GetSOValue()
    {
        switch (targetValue)
        {
            case TargetValue.MouseSensitivityX:
                return sensitivitySO.MouseSensitivityX;

            case TargetValue.MouseSensitivityY:
                return sensitivitySO.MouseSensitivityY;

            case TargetValue.WeaponSensitivityX:
                return sensitivitySO.WeaponSensitivityX;

            case TargetValue.WeaponSensitivityY:
                return sensitivitySO.WeaponSensitivityY;

            default:
                return 0f;
        }
    }
}
