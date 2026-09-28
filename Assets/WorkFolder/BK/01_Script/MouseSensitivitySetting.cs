using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MouseSensitivitySetting : MonoBehaviour
{
    [SerializeField] private TMP_InputField sensitivityInput;

    [SerializeField] private float minSensitivity = 0.1f;
    [SerializeField] private float maxSensitivity = 10f;
    [SerializeField] private float defaultSensitivity = 1f;

    private UnityEngine.UI.Slider sensitivitySlider;
    private SliderSnap sliderSnap;

    private const string SensitivityKey = "MouseSensitivity";

    public float Sensitivity { get; private set; } = 1f;

    private void Awake()
    {
        sensitivitySlider = GetComponent<UnityEngine.UI.Slider>();

        if (sensitivityInput == null)
        {
            Debug.LogError("Inspector에서 숫자 입력창을 연결해주세요.");
            return;
        }

        sensitivitySlider.minValue = minSensitivity;
        sensitivitySlider.maxValue = maxSensitivity;
        sliderSnap = SliderSnap.Attach(sensitivitySlider, 0.1f);

        sensitivitySlider.onValueChanged.AddListener(OnSliderChanged);
        sensitivityInput.onEndEdit.AddListener(OnInputChanged);

        float savedSensitivity = PlayerPrefs.GetFloat(
            SensitivityKey,
            defaultSensitivity
        );

        ApplySensitivity(savedSensitivity);
    }

    private void OnDestroy()
    {
        if (sensitivitySlider != null)
        {
            sensitivitySlider.onValueChanged.RemoveListener(OnSliderChanged);
        }

        if (sensitivityInput != null)
        {
            sensitivityInput.onEndEdit.RemoveListener(OnInputChanged);
        }
    }

    private void OnSliderChanged(float value)
    {
        ApplySensitivity(value);
    }

    private void OnInputChanged(string text)
    {
        if (float.TryParse(text, out float value))
        {
            ApplySensitivity(value);
        }
        else
        {
            sensitivityInput.SetTextWithoutNotify(
                Sensitivity.ToString("0.0")
            );
        }
    }

    private void ApplySensitivity(float value)
    {
        Sensitivity = sliderSnap.SnapAndSet(Mathf.Clamp(
            value,
            minSensitivity,
            maxSensitivity
        ));

        sensitivityInput.SetTextWithoutNotify(
            Sensitivity.ToString("0.0")
        );

        PlayerPrefs.SetFloat(SensitivityKey, Sensitivity);

        Debug.Log($"현재 마우스 감도: {Sensitivity}");
    }
}
