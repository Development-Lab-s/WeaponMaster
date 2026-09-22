using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WorkFolder.PAP.Scripts.SO.DataFolder;

[RequireComponent(typeof(Image))]
public class SensitivityCircleUI : MonoBehaviour, //몇번째 바이브지 뇌가 녹아버릴거 같아아
    IPointerDownHandler,
    IDragHandler,
    ICanvasRaycastFilter
{
    public enum TargetType
    {
        Mouse,
        Weapon
    }

    [Header("변경할 SO와 대상")]
    [SerializeField] private SettingValueSO settingSO;
    [SerializeField] private TargetType targetType;

    [Header("UI")]
    [SerializeField] private RectTransform handle;
    [SerializeField] private TMP_Text valueText;

    [Header("감도 범위: 최소 < 중앙 < 최대")]
    [SerializeField] private float minSensitivity = 0.1f;
    [SerializeField] private float centerSensitivity = 1f;
    [SerializeField] private float maxSensitivity = 3f;

    private RectTransform Area => (RectTransform)transform;
    private bool IsReady => settingSO != null && handle != null;

    // 핸들이 배경 원 밖으로 튀어나가지 않도록 크기만큼 제외합니다.
    private float MoveRadius => Mathf.Max(
        1f,
        Mathf.Min(Area.rect.width, Area.rect.height) * 0.5f
        - Mathf.Max(handle.rect.width, handle.rect.height) * 0.5f
    );

    private void OnValidate()
    {
        minSensitivity = Mathf.Max(0.001f, minSensitivity);
        centerSensitivity =
            Mathf.Max(minSensitivity + 0.001f, centerSensitivity);
        maxSensitivity =
            Mathf.Max(centerSensitivity + 0.001f, maxSensitivity);
    }

    private void OnEnable()
    {
        RefreshFromSO();
    }

    private void Start()
    {
        // 처음 활성화될 때 UI 레이아웃이 반영된 크기로 갱신합니다.
        RefreshFromSO();
    }

    private void OnRectTransformDimensionsChange()
    {
        RefreshFromSO();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        ApplyPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        ApplyPointer(eventData);
    }

    private void ApplyPointer(PointerEventData eventData)
    {
        if (!IsReady ||
            eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                Area,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
            return;

        // 원 중심을 기준으로 -1 ~ 1 좌표를 구합니다.
        Vector2 normalized = Vector2.ClampMagnitude(
            (localPoint - Area.rect.center) / MoveRadius,
            1f
        );

        Vector2 sensitivity = new Vector2(
            Mathf.Round(PositionToSensitivity(normalized.x) * 100f) / 100f,
            Mathf.Round(PositionToSensitivity(normalized.y) * 100f) / 100f
        );

        if (targetType == TargetType.Mouse)
        {
            settingSO.MouseSensitivityX = sensitivity.x;
            settingSO.MouseSensitivityY = sensitivity.y;
        }
        else
        {
            settingSO.WeaponSensitivityX = sensitivity.x;
            settingSO.WeaponSensitivityY = sensitivity.y;
        }

        UpdateVisual(normalized, sensitivity);
    }

    // SO 값을 읽어 점 위치를 맞춥니다. SO 자체는 변경하지 않습니다.
    public void RefreshFromSO()
    {
        if (!IsReady)
            return;

        Vector2 sensitivity = targetType == TargetType.Mouse
            ? new Vector2(
                settingSO.MouseSensitivityX,
                settingSO.MouseSensitivityY)
            : new Vector2(
                settingSO.WeaponSensitivityX,
                settingSO.WeaponSensitivityY);

        Vector2 normalized = new Vector2(
            SensitivityToPosition(sensitivity.x),
            SensitivityToPosition(sensitivity.y)
        );

        UpdateVisual(
            Vector2.ClampMagnitude(normalized, 1f),
            sensitivity
        );
    }

    // 위치 -1 → 최소, 0 → 중앙, +1 → 최대
    private float PositionToSensitivity(float position)
    {
        return position < 0f
            ? Mathf.Lerp(centerSensitivity, minSensitivity, -position)
            : Mathf.Lerp(centerSensitivity, maxSensitivity, position);
    }

    private float SensitivityToPosition(float sensitivity)
    {
        return sensitivity < centerSensitivity
            ? -Mathf.InverseLerp(
                centerSensitivity, minSensitivity, sensitivity)
            : Mathf.InverseLerp(
                centerSensitivity, maxSensitivity, sensitivity);
    }

    private void UpdateVisual(
        Vector2 normalized,
        Vector2 sensitivity)
    {
        handle.anchoredPosition = normalized * MoveRadius;

        if (valueText != null)
        {
            valueText.text =
                $"X : {sensitivity.x:F2}\nY : {sensitivity.y:F2}";
        }
    }

    // Image의 사각 영역 중 원 밖의 모서리는 클릭 대상에서 제외합니다.
    public bool IsRaycastLocationValid(
        Vector2 screenPoint,
        Camera eventCamera)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                Area, screenPoint, eventCamera, out Vector2 localPoint))
            return false;

        float radius =
            Mathf.Min(Area.rect.width, Area.rect.height) * 0.5f;

        return (localPoint - Area.rect.center).sqrMagnitude
            <= radius * radius;
    }
}