using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[DefaultExecutionOrder(200)]
public class BKWeaponScroll : MonoBehaviour, IScrollHandler //나는 바이브 인간 입니다.
{
    [Serializable]
    public class WeaponEntry
    {
        [Tooltip("원통과 실제 무기 모델을 묶은 WeaponRow")]
        public Transform visualRoot;

        [Tooltip("같은 무기를 장착하는 기존 WeaponSlot")]
        public InventorySlotUI dragSlot;
    }

    [Header("연결")]
    [SerializeField] private Camera previewCamera;
    [SerializeField] private RawImage previewImage;
    [SerializeField] private WeaponEntry[] items;

    [Header("배치")]
    [Range(1f, 5f)]
    [SerializeField] private float visibleCount = 2.5f;

    [Range(0f, 1f)]
    [SerializeField] private float horizontalPosition = 0.5f;

    [Min(0.1f)]
    [SerializeField] private float cameraDistance = 10f;

    [Header("위쪽 / 아래쪽 크기")]
    [Min(0.05f)]
    [SerializeField] private float topScale = 0.7f;

    [Min(0.05f)]
    [SerializeField] private float bottomScale = 1.3f;

    [Header("스크롤")]
    [SerializeField] private float wheelStep = 1f;

    [Min(0.1f)]
    [SerializeField] private float smoothSpeed = 10f;

    [Min(0.1f)]
    [SerializeField] private float maxRowsPerSecond = 6f;

    private class RowState
    {
        public WeaponEntry entry;
        public Renderer[] renderers;
        public Vector3 localCenter;
        public Vector3 originalPosition;
        public Vector3 originalScale;

        public RectTransform slotRect;
        public Vector3 originalSlotPosition;
        public Quaternion originalSlotRotation;
        public Vector2 originalSlotSize;

        public Image hitImage;
        public bool originalHitRaycast;

        public RawImage[] oldIcons;
        public bool[] originalIconEnabled;
    }

    private RowState[] states;
    private bool initialized;
    private bool originalPreviewRaycast;

    private float scrollOffset;
    private float pendingScroll;
    private float largestBaseHeight;

    private void Start()
    {
        Canvas.ForceUpdateCanvases();

        if (!Prepare())
        {
            enabled = false;
            return;
        }

        originalPreviewRaycast = previewImage.raycastTarget;
        initialized = true;
        ActivatePresentation();
    }

    private bool Prepare()
    {
        if (previewCamera == null || previewImage == null)
            return SetupError("Preview Camera와 Preview Image를 연결하세요.");

        if (!previewCamera.orthographic)
            return SetupError("ObjCamera의 Projection을 Orthographic으로 설정하세요.");

        if (previewCamera.targetTexture == null ||
            previewImage.texture != previewCamera.targetTexture)
        {
            return SetupError(
                "카메라 Output Texture와 ObjImage Texture에 같은 RTObj를 연결하세요.");
        }

        if (previewImage.uvRect.width <= 0 ||
            previewImage.uvRect.height <= 0)
        {
            return SetupError("ObjImage의 UV Rect Width와 Height는 양수여야 합니다.");
        }

        if (cameraDistance <= previewCamera.nearClipPlane ||
            cameraDistance >= previewCamera.farClipPlane)
        {
            return SetupError("Camera Distance를 카메라의 Near와 Far 사이로 설정하세요.");
        }

        if (items == null || items.Length < 2)
            return SetupError("Items에 최소 2개의 무기 칸을 연결하세요.");

        states = new RowState[items.Length];

        var usedRoots = new HashSet<Transform>();
        var usedSlots = new HashSet<InventorySlotUI>();

        for (int i = 0; i < items.Length; i++)
        {
            WeaponEntry entry = items[i];

            if (entry == null ||
                entry.visualRoot == null ||
                entry.dragSlot == null)
            {
                return SetupError($"Items의 {i}번 연결이 비어 있습니다.");
            }

            if (!usedRoots.Add(entry.visualRoot) ||
                !usedSlots.Add(entry.dragSlot))
            {
                return SetupError("같은 WeaponRow나 WeaponSlot을 중복 연결하지 마세요.");
            }

            if (entry.visualRoot is RectTransform ||
                previewCamera.transform.IsChildOf(entry.visualRoot))
            {
                return SetupError("Visual Root에는 카메라/UI가 아닌 WeaponRow를 넣으세요.");
            }

            if (!entry.dragSlot.transform.IsChildOf(transform) ||
                entry.dragSlot.transform == transform)
            {
                return SetupError("스크립트를 WeaponListPanel에 붙이세요.");
            }

            if (previewImage.transform.IsChildOf(entry.dragSlot.transform))
            {
                return SetupError("Preview Image에는 슬롯 아이콘이 아닌 전체 ObjImage를 넣으세요.");
            }

            for (int j = 0; j < i; j++)
            {
                Transform other = items[j].visualRoot;

                if (entry.visualRoot.IsChildOf(other) ||
                    other.IsChildOf(entry.visualRoot))
                {
                    return SetupError("WeaponRow들은 서로의 자식이 아닌 나란한 오브젝트여야 합니다.");
                }
            }

            Image hitImage = entry.dragSlot.GetComponent<Image>();

            if (hitImage == null)
                return SetupError($"{entry.dragSlot.name}에 입력용 Image가 없습니다.");

            Renderer[] renderers =
                entry.visualRoot.GetComponentsInChildren<Renderer>(true);

            if (!TryGetBounds(renderers, out Bounds bounds))
            {
                return SetupError(
                    $"{entry.visualRoot.name}의 모델과 Layer를 확인하세요. " +
                    "카메라가 볼 수 있는 활성 Renderer가 필요합니다.");
            }

            Rect projected = ProjectBounds(bounds);

            // 카메라 Size가 바뀌어도 사용할 수 있도록 월드 높이로 보관.
            largestBaseHeight = Mathf.Max(
                largestBaseHeight,
                projected.height * 2f * previewCamera.orthographicSize);

            RectTransform slotRect =
                (RectTransform)entry.dragSlot.transform;

            RawImage[] icons =
                entry.dragSlot.GetComponentsInChildren<RawImage>(true);

            bool[] iconEnabled = new bool[icons.Length];

            for (int j = 0; j < icons.Length; j++)
                iconEnabled[j] = icons[j].enabled;

            states[i] = new RowState
            {
                entry = entry,
                renderers = renderers,

                localCenter =
                    entry.visualRoot.InverseTransformPoint(bounds.center),

                originalPosition = entry.visualRoot.localPosition,
                originalScale = entry.visualRoot.localScale,

                slotRect = slotRect,
                originalSlotPosition = slotRect.anchoredPosition3D,
                originalSlotRotation = slotRect.localRotation,
                originalSlotSize = slotRect.sizeDelta,

                hitImage = hitImage,
                originalHitRaycast = hitImage.raycastTarget,

                oldIcons = icons,
                originalIconEnabled = iconEnabled
            };
        }

        return true;
    }

    private bool SetupError(string message)
    {
        Debug.LogError("[BKWeaponScroll] " + message, this);
        return false;
    }

    private void OnEnable()
    {
        if (initialized)
            ActivatePresentation();
    }

    private void ActivatePresentation()
    {
        // 전체 촬영 이미지는 클릭을 가로채지 않도록 한다.
        previewImage.raycastTarget = false;

        // 기존 아이콘 대신 함께 촬영한 실제 3D 모델을 표시한다.
        foreach (RowState state in states)
        {
            foreach (RawImage icon in state.oldIcons)
            {
                if (icon != null)
                    icon.enabled = false;
            }
        }

        UpdateLayout();
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (!initialized || IsDragging())
            return;

        float wheel = eventData.scrollDelta.y;

        if (Mathf.Approximately(wheel, 0f))
            return;

        // 휠을 내리면 목록이 위로 이동한다.
        // 입력 모듈에 따른 휠 값 차이를 줄이기 위해 방향만 사용한다.
        pendingScroll -= Mathf.Sign(wheel) * wheelStep;
        eventData.Use();
    }

    private bool IsDragging()
    {
        InventoryDragController drag = InventoryDragController.Instance;
        return drag != null && drag.IsDragging;
    }

    private void LateUpdate()
    {
        if (!initialized)
            return;

        if (IsDragging())
        {
            // 무기를 놓은 뒤 밀린 스크롤이 실행되지 않도록 취소.
            pendingScroll = 0f;
            return;
        }

        float deltaTime = Time.unscaledDeltaTime;

        float movement = pendingScroll *
                         (1f - Mathf.Exp(-smoothSpeed * deltaTime));

        if (Mathf.Abs(pendingScroll) < 0.001f)
            movement = pendingScroll;

        float limit = maxRowsPerSecond * deltaTime;
        movement = Mathf.Clamp(movement, -limit, limit);

        pendingScroll -= movement;

        scrollOffset = Mathf.Repeat(
            scrollOffset + movement,
            states.Length);

        UpdateLayout();
    }

    private void UpdateLayout()
    {
        Rect uv = previewImage.uvRect;

        float largestHeight =
            largestBaseHeight /
            (2f * previewCamera.orthographicSize * uv.height);

        largestHeight *= Mathf.Max(topScale, bottomScale);

        const float padding = 0.05f;

        // 확대된 칸끼리 겹치지 않고, 완전히 화면 밖에서 순환하도록
        // 필요한 공간을 확보한다.
        float cycleHeight = Mathf.Max(
            states.Length / visibleCount,
            states.Length * (largestHeight + padding),
            1f + largestHeight + padding * 2f);

        float spacing = cycleHeight / states.Length;
        float lowerLimit = (1f - cycleHeight) * 0.5f;

        for (int i = 0; i < states.Length; i++)
        {
            RowState state = states[i];

            if (state.entry.visualRoot == null ||
                state.slotRect == null ||
                state.hitImage == null)
            {
                if (state.hitImage != null)
                    state.hitImage.raycastTarget = false;

                continue;
            }

            float y = 1f - (i + 0.5f) * spacing;
            y += scrollOffset * spacing;

            y = lowerLimit +
                Mathf.Repeat(y - lowerLimit, cycleHeight);

            // y=1은 위, y=0은 아래.
            float t = Mathf.Clamp01(1f - y);
            t = Mathf.SmoothStep(0f, 1f, t);

            float scale = Mathf.Lerp(topScale, bottomScale, t);

            Transform root = state.entry.visualRoot;
            root.localScale = state.originalScale * scale;

            Vector3 center = previewCamera.ViewportToWorldPoint(
                new Vector3(
                    uv.x + horizontalPosition * uv.width,
                    uv.y + y * uv.height,
                    cameraDistance));

            // 모델 원점이 아니라 실제 모델의 중심을 기준으로 배치.
            root.position =
                center - root.TransformVector(state.localCenter);

            UpdateHitArea(state);
        }
    }

    private void UpdateHitArea(RowState state)
    {
        if (!TryGetBounds(state.renderers, out Bounds bounds))
        {
            state.hitImage.raycastTarget = false;
            return;
        }

        Rect projected = ProjectBounds(bounds);
        Rect uv = previewImage.uvRect;

        float xMin = Mathf.Clamp01((projected.xMin - uv.x) / uv.width);
        float xMax = Mathf.Clamp01((projected.xMax - uv.x) / uv.width);
        float yMin = Mathf.Clamp01((projected.yMin - uv.y) / uv.height);
        float yMax = Mathf.Clamp01((projected.yMax - uv.y) / uv.height);

        bool visible = xMax > xMin && yMax > yMin;

        state.hitImage.raycastTarget =
            visible && state.originalHitRaycast;

        if (!visible)
            return;

        RectTransform imageRect = previewImage.rectTransform;
        Rect imageBounds = imageRect.rect;

        Rect hit = Rect.MinMaxRect(
            imageBounds.xMin + xMin * imageBounds.width,
            imageBounds.yMin + yMin * imageBounds.height,
            imageBounds.xMin + xMax * imageBounds.width,
            imageBounds.yMin + yMax * imageBounds.height);

        RectTransform slot = state.slotRect;
        slot.rotation = imageRect.rotation;

        Vector3 widthVector = slot.InverseTransformVector(
            imageRect.TransformVector(new Vector3(hit.width, 0f, 0f)));

        Vector3 heightVector = slot.InverseTransformVector(
            imageRect.TransformVector(new Vector3(0f, hit.height, 0f)));

        slot.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            Mathf.Abs(widthVector.x));

        slot.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            Mathf.Abs(heightVector.y));

        slot.position = imageRect.TransformPoint(
            new Vector3(
                hit.xMin + hit.width * slot.pivot.x,
                hit.yMin + hit.height * slot.pivot.y,
                0f));
    }

    private bool TryGetBounds(Renderer[] renderers, out Bounds bounds)
    {
        bounds = default;
        bool found = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null ||
                !renderer.enabled ||
                !renderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            int layerBit = 1 << renderer.gameObject.layer;

            if ((previewCamera.cullingMask & layerBit) == 0)
                continue;

            if (!found)
                bounds = renderer.bounds;
            else
                bounds.Encapsulate(renderer.bounds);

            found = true;
        }

        return found;
    }

    private Rect ProjectBounds(Bounds bounds)
    {
        Vector2 min = new Vector2(
            float.PositiveInfinity, float.PositiveInfinity);

        Vector2 max = new Vector2(
            float.NegativeInfinity, float.NegativeInfinity);

        for (int i = 0; i < 8; i++)
        {
            Vector3 direction = new Vector3(
                (i & 1) == 0 ? -1f : 1f,
                (i & 2) == 0 ? -1f : 1f,
                (i & 4) == 0 ? -1f : 1f);

            Vector3 corner = bounds.center +
                             Vector3.Scale(bounds.extents, direction);

            Vector3 projected = previewCamera.WorldToViewportPoint(corner);

            min = Vector2.Min(min, (Vector2)projected);
            max = Vector2.Max(max, (Vector2)projected);
        }

        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private void OnDisable()
    {
        if (!initialized)
            return;

        pendingScroll = 0f;

        if (previewImage != null)
            previewImage.raycastTarget = originalPreviewRaycast;

        foreach (RowState state in states)
        {
            if (state.entry.visualRoot != null)
            {
                state.entry.visualRoot.localPosition =
                    state.originalPosition;

                state.entry.visualRoot.localScale =
                    state.originalScale;
            }

            if (state.slotRect != null)
            {
                state.slotRect.localRotation = state.originalSlotRotation;
                state.slotRect.sizeDelta = state.originalSlotSize;
                state.slotRect.anchoredPosition3D = state.originalSlotPosition;
            }

            if (state.hitImage != null)
                state.hitImage.raycastTarget = state.originalHitRaycast;

            for (int i = 0; i < state.oldIcons.Length; i++)
            {
                if (state.oldIcons[i] != null)
                    state.oldIcons[i].enabled = state.originalIconEnabled[i];
            }
        }
    }

    private void OnValidate()
    {
        visibleCount = Mathf.Max(1f, visibleCount);
        topScale = Mathf.Max(0.05f, topScale);
        bottomScale = Mathf.Max(0.05f, bottomScale);
        smoothSpeed = Mathf.Max(0.1f, smoothSpeed);
        maxRowsPerSecond = Mathf.Max(0.1f, maxRowsPerSecond);
    }
}