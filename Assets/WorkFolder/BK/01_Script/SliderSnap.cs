using UnityEngine;

[RequireComponent(typeof(UnityEngine.UI.Slider))]
public sealed class SliderSnap : MonoBehaviour
{
    [SerializeField, Min(0.0001f)] private float step = 0.1f;

    private UnityEngine.UI.Slider slider;

    public static SliderSnap Attach(UnityEngine.UI.Slider target, float stepSize)
    {
        float originalValue = target.value;
        SliderSnap snap = target.GetComponent<SliderSnap>();
        if (snap == null)
            snap = target.gameObject.AddComponent<SliderSnap>();

        snap.step = Mathf.Max(0.0001f, stepSize);
        snap.SnapAndSet(originalValue);
        return snap;
    }

    private void Awake()
    {
        slider = GetComponent<UnityEngine.UI.Slider>();
    }

    private void OnEnable()
    {
        if (slider == null)
            slider = GetComponent<UnityEngine.UI.Slider>();

        slider.onValueChanged.AddListener(OnValueChanged);
        SnapAndSet(slider.value);
    }

    private void OnDisable()
    {
        if (slider != null)
            slider.onValueChanged.RemoveListener(OnValueChanged);
    }

    private void OnValueChanged(float value)
    {
        SnapAndSet(value);
    }

    public float SnapAndSet(float value)
    {
        if (slider == null)
            slider = GetComponent<UnityEngine.UI.Slider>();

        float min = slider.minValue;
        float max = slider.maxValue;
        float clamped = Mathf.Clamp(value, min, max);
        float snapped = min + Mathf.Round((clamped - min) / step) * step;
        snapped = Mathf.Clamp(snapped, min, max);

        // 끝값도 선택할 수 있게 합니다. 범위가 step의 배수가 아닌 경우에 필요합니다.
        if (max - clamped < step * 0.5f)
            snapped = max;

        slider.SetValueWithoutNotify(snapped);
        return snapped;
    }
}
