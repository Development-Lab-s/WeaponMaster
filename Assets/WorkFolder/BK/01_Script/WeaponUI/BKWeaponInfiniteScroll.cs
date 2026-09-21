using System;
using System.Collections.Generic;
using BK.WeaponUI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Put this on WeaponListPanel. Each 3D row keeps its original InventorySlotUI;
/// only presentation and hit rectangles move. Inventory bindings never change.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[DefaultExecutionOrder(200)]
public sealed class BKWeaponInfiniteScroll : MonoBehaviour, IScrollHandler
{
    [Serializable]
    public sealed class WeaponRow
    {
        [Tooltip("원통과 실제 무기 모델을 함께 담은 WeaponRow 오브젝트")]
        public Transform visualRoot;
        [Tooltip("같은 무기를 장착하는 기존 WeaponSlot. 모델과 슬롯의 무기 종류를 맞추세요.")]
        public InventorySlotUI dragSlot;
    }

    [Header("촬영 화면")]
    [SerializeField] private Camera previewCamera;
    [SerializeField] private RawImage previewImage;
    [Header("무기 순서 / 드래그 슬롯 연결")]
    [SerializeField] private WeaponRow[] rows = Array.Empty<WeaponRow>();

    [Header("배치")]
    [Tooltip("한 화면에 배치할 칸 수. 큰 모델은 화면 밖에서 순환하도록 간격을 자동으로 넓힙니다.")]
    [Range(1f, 5f)] [SerializeField] private float visibleRows = 2.5f;
    [Range(0f, 1f)] [SerializeField] private float horizontalPosition = 0.5f;
    [Min(0.1f)] [SerializeField] private float cameraDistance = 10f;
    [Min(0f)] [SerializeField] private float recyclePadding = 0.05f;
    [Tooltip("슬롯 클릭 영역에 추가하는 여백. ObjImage의 UI 좌표 기준입니다.")]
    [SerializeField] private Vector2 hitPadding = new Vector2(8f, 8f);

    [Header("위에서 아래로 커지는 효과")]
    [Min(0.05f)] [SerializeField] private float topScale = 0.7f;
    [Min(0.05f)] [SerializeField] private float bottomScale = 1.3f;

    [Header("휠 이동")]
    [Tooltip("휠 이벤트 한 번에 이동할 칸 수. 양수이면 휠을 내릴 때 목록이 위로 움직입니다.")]
    [SerializeField] private float rowsPerScroll = 1f;
    [Min(0.1f)] [SerializeField] private float smoothResponse = 10f;
    [Min(0.1f)] [SerializeField] private float maxRowsPerSecond = 6f;

    private sealed class RowState
    {
        public WeaponRow row;
        public Renderer[] renderers;
        public Vector3 centerInRoot;
        public Vector3 originalPosition, originalScale;
        public RectTransform slotRect;
        public Vector3 slotPosition, slotScale;
        public Quaternion slotRotation;
        public Vector2 slotSize;
        public RawImage[] oldIcons;
        public bool[] iconEnabled;
        public CanvasGroup group;
        public bool ownsGroup, groupInteractable, groupBlocks;
        public float groupAlpha;
    }

    private RowState[] states;
    private RectTransform wheelArea;
    private bool prepared, originalImageRaycast;
    private float largestRowHeight;
    private double offset, pending;

    private void Reset()
    {
        AutoFillReferences();
    }

    [ContextMenu("Auto Fill References (현재 씬 자동 연결)")]
    private void AutoFillReferences()
    {
        if (Application.isPlaying) return;

        if (previewCamera == null)
        {
            foreach (var candidate in FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (candidate.gameObject.scene == gameObject.scene && candidate.name == "ObjCamera")
                { previewCamera = candidate; break; }
        }

        if (previewImage == null)
        {
            foreach (var candidate in GetComponentsInChildren<RawImage>(true))
                if (candidate.name.StartsWith("ObjImage", StringComparison.Ordinal))
                { previewImage = candidate; break; }
        }

        if (previewCamera == null || previewCamera.transform.parent == null) return;
        var roots = new List<Transform>();
        foreach (Transform child in previewCamera.transform.parent)
            if (child.name.StartsWith("WeaponRow_", StringComparison.Ordinal)) roots.Add(child);
        roots.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        var slots = GetComponentsInChildren<InventorySlotUI>(true);
        rows = new WeaponRow[roots.Count];
        for (int i = 0; i < rows.Length; i++)
            rows[i] = new WeaponRow { visualRoot = roots[i], dragSlot = i < slots.Length ? slots[i] : null };
    }

    private void Start()
    {
        if (!ValidateSetup()) { enabled = false; return; }
        states = new RowState[rows.Length];
        largestRowHeight = 0;

        // Capture everything before changing graphics or transforms.
        for (int i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            var renderers = row.visualRoot.GetComponentsInChildren<Renderer>(true);
            if (!TryBounds(renderers, out var worldBounds, out var viewBounds))
            {
                Fail($"Rows의 {i + 1}번째 모델이 카메라 앞에 보이지 않습니다. 원통과 무기 위치/Layer를 확인하세요.");
                enabled = false;
                return;
            }

            var rect = (RectTransform)row.dragSlot.transform;
            var icons = row.dragSlot.GetComponentsInChildren<RawImage>(true);
            var iconEnabled = new bool[icons.Length];
            for (int j = 0; j < icons.Length; j++) iconEnabled[j] = icons[j].enabled;
            var group = rect.GetComponent<CanvasGroup>();
            states[i] = new RowState
            {
                row = row, renderers = renderers,
                centerInRoot = row.visualRoot.InverseTransformPoint(worldBounds.center),
                originalPosition = row.visualRoot.localPosition, originalScale = row.visualRoot.localScale,
                slotRect = rect, slotPosition = rect.anchoredPosition3D, slotScale = rect.localScale,
                slotRotation = rect.localRotation, slotSize = rect.sizeDelta,
                oldIcons = icons, iconEnabled = iconEnabled, group = group, ownsGroup = group == null,
                groupAlpha = group != null ? group.alpha : 1,
                groupInteractable = group == null || group.interactable,
                groupBlocks = group == null || group.blocksRaycasts
            };
            largestRowHeight = Mathf.Max(largestRowHeight, viewBounds.height / previewImage.uvRect.height);
        }

        foreach (var state in states)
            if (state.group == null) state.group = state.slotRect.gameObject.AddComponent<CanvasGroup>();

        // This background is behind the slots, never on top of them. Wheel events
        // bubble to this panel; InventorySlotUI continues to handle drag/drop.
        var area = new GameObject("BK Scroll Input Area", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        wheelArea = (RectTransform)area.transform;
        wheelArea.SetParent(transform, false);
        wheelArea.SetAsFirstSibling();
        var image = area.GetComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = true;
        originalImageRaycast = previewImage.raycastTarget;
        prepared = true;
        Activate();
    }

    private bool ValidateSetup()
    {
        if (previewCamera == null || previewImage == null)
            return Fail("Preview Camera와 Preview Image를 연결하세요. 컴포넌트 메뉴의 Auto Fill References를 사용할 수 있습니다.");
        if (!previewCamera.orthographic)
            return Fail("ObjCamera의 Projection을 Orthographic으로 설정하세요.");
        if (previewCamera.targetTexture == null || previewImage.texture != previewCamera.targetTexture)
            return Fail("ObjCamera의 Output Texture와 ObjImage의 Texture에 같은 RTObj를 연결하세요.");
        if (previewImage.uvRect.width <= 0 || previewImage.uvRect.height <= 0)
            return Fail("ObjImage의 UV Rect Width와 Height는 양수여야 합니다.");
        if (cameraDistance <= previewCamera.nearClipPlane || cameraDistance >= previewCamera.farClipPlane)
            return Fail("Camera Distance를 ObjCamera의 Near와 Far 사이로 설정하세요.");
        if (rows == null || rows.Length < 2)
            return Fail("Rows에 원통+무기 세트와 기존 드래그 슬롯을 최소 2쌍 연결하세요.");

        var roots = new HashSet<Transform>();
        var slots = new HashSet<InventorySlotUI>();
        foreach (var row in rows)
        {
            if (row == null || row.visualRoot == null || row.dragSlot == null)
                return Fail("Rows의 모든 Visual Root와 Drag Slot을 연결하세요.");
            if (!roots.Add(row.visualRoot) || !slots.Add(row.dragSlot))
                return Fail("같은 Visual Root 또는 Drag Slot을 두 번 연결할 수 없습니다.");
            if (!row.dragSlot.transform.IsChildOf(transform) || row.dragSlot.transform == transform)
                return Fail("이 스크립트를 기존 WeaponSlot들을 포함하는 WeaponListPanel에 붙이세요.");
            if (previewImage.transform.IsChildOf(row.dragSlot.transform))
                return Fail("ObjImage를 WeaponSlot 밖에 두세요. 기존 무기 아이콘을 Preview Image로 연결하면 안 됩니다.");
            if (previewCamera.transform.IsChildOf(row.visualRoot) || row.visualRoot is RectTransform)
                return Fail("Visual Root에는 카메라/UI가 아닌 3D WeaponRow를 연결하세요.");
        }
        foreach (var a in rows)
            foreach (var b in rows)
                if (a != b && a.visualRoot.IsChildOf(b.visualRoot))
                    return Fail("WeaponRow들을 서로의 자식으로 넣지 말고 나란히 배치하세요.");
        return true;
    }

    private bool Fail(string message)
    {
        Debug.LogError($"[BKWeaponInfiniteScroll] {message}", this);
        return false;
    }

    private void OnEnable()
    {
        if (prepared) Activate();
    }

    private void Activate()
    {
        previewImage.raycastTarget = false;
        wheelArea.gameObject.SetActive(true);
        foreach (var state in states)
            foreach (var icon in state.oldIcons)
                if (icon != null) icon.enabled = false;
        ApplyLayout();
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (!prepared || IsDragging() || Mathf.Approximately(eventData.scrollDelta.y, 0)) return;
        // Event magnitudes differ between legacy and Input System UI modules.
        pending -= Math.Sign(eventData.scrollDelta.y) * rowsPerScroll;
        eventData.Use();
    }

    private static bool IsDragging()
    {
        var drag = InventoryDragController.Instance;
        return drag != null && drag.IsDragging;
    }

    private void LateUpdate()
    {
        if (!prepared) return;
        BKWeaponScrollMath.Advance(ref offset, ref pending, states.Length, smoothResponse,
            maxRowsPerSecond, Time.unscaledDeltaTime, IsDragging());
        ApplyLayout();
    }

    private void ApplyLayout()
    {
        var imageRect = previewImage.rectTransform;
        var uv = previewImage.uvRect;
        MatchImageRect(wheelArea, imageRect.rect);
        double cycle = BKWeaponScrollMath.CycleLength(states.Length, Mathf.Max(1, visibleRows),
            largestRowHeight, Mathf.Max(topScale, bottomScale), recyclePadding);

        for (int i = 0; i < states.Length; i++)
        {
            var state = states[i];
            if (state.row.visualRoot == null || state.slotRect == null || state.group == null) continue;
            float y = (float)BKWeaponScrollMath.RowY(i, states.Length, offset, cycle);
            float scale = BKWeaponScrollMath.ScaleAt(y, topScale, bottomScale);
            var root = state.row.visualRoot;
            root.localScale = state.originalScale * scale;
            var center = previewCamera.ViewportToWorldPoint(new Vector3(
                uv.x + horizontalPosition * uv.width, uv.y + y * uv.height, cameraDistance));
            root.position = center - root.TransformVector(state.centerInRoot);

            bool visible = TryBounds(state.renderers, out _, out var bounds);
            Rect hit = default;
            if (visible)
            {
                float xMin = Mathf.Max(0, (bounds.xMin - uv.x) / uv.width);
                float xMax = Mathf.Min(1, (bounds.xMax - uv.x) / uv.width);
                float yMin = Mathf.Max(0, (bounds.yMin - uv.y) / uv.height);
                float yMax = Mathf.Min(1, (bounds.yMax - uv.y) / uv.height);
                visible = xMax > xMin && yMax > yMin;
                if (visible)
                {
                    var r = imageRect.rect;
                    hit = Rect.MinMaxRect(
                        Mathf.Max(r.xMin, r.xMin + xMin * r.width - hitPadding.x),
                        Mathf.Max(r.yMin, r.yMin + yMin * r.height - hitPadding.y),
                        Mathf.Min(r.xMax, r.xMin + xMax * r.width + hitPadding.x),
                        Mathf.Min(r.yMax, r.yMin + yMax * r.height + hitPadding.y));
                    MatchImageRect(state.slotRect, hit);
                }
            }
            state.group.alpha = visible ? state.groupAlpha : 0;
            state.group.interactable = visible && state.groupInteractable;
            state.group.blocksRaycasts = visible && state.groupBlocks;
        }
    }

    // Bounds are projected through the actual preview camera, then through the
    // RawImage's UV rectangle. Canvas scaling and anchors are not assumed.
    private bool TryBounds(Renderer[] renderers, out Bounds worldBounds, out Rect viewportBounds)
    {
        worldBounds = default;
        viewportBounds = default;
        bool found = false;
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        foreach (var renderer in renderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                (previewCamera.cullingMask & (1 << renderer.gameObject.layer)) == 0) continue;
            var b = renderer.bounds;
            if (!found) worldBounds = b; else worldBounds.Encapsulate(b);
            found = true;
            for (int corner = 0; corner < 8; corner++)
            {
                var p = b.center + Vector3.Scale(b.extents, new Vector3(
                    (corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                var v = previewCamera.WorldToViewportPoint(p);
                if (v.z <= 0) return false;
                min = Vector2.Min(min, (Vector2)v);
                max = Vector2.Max(max, (Vector2)v);
            }
        }
        if (found) viewportBounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return found;
    }

    private void MatchImageRect(RectTransform target, Rect rectangle)
    {
        var image = previewImage.rectTransform;
        target.rotation = image.rotation;
        // Keep each slot's parent and sibling order: InventoryUI binds by child order.
        Vector3 sizeX = target.InverseTransformVector(image.TransformVector(new Vector3(rectangle.width, 0, 0)));
        Vector3 sizeY = target.InverseTransformVector(image.TransformVector(new Vector3(0, rectangle.height, 0)));
        target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Abs(sizeX.x));
        target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Abs(sizeY.y));
        target.position = image.TransformPoint(new Vector3(
            rectangle.xMin + rectangle.width * target.pivot.x,
            rectangle.yMin + rectangle.height * target.pivot.y, 0));
    }

    private void OnDisable()
    {
        if (!prepared) return;
        pending = 0;
        if (previewImage != null) previewImage.raycastTarget = originalImageRaycast;
        if (wheelArea != null) wheelArea.gameObject.SetActive(false);
        foreach (var state in states)
        {
            if (state.row.visualRoot != null)
            {
                state.row.visualRoot.localPosition = state.originalPosition;
                state.row.visualRoot.localScale = state.originalScale;
            }
            if (state.slotRect != null)
            {
                state.slotRect.localScale = state.slotScale;
                state.slotRect.localRotation = state.slotRotation;
                state.slotRect.sizeDelta = state.slotSize;
                state.slotRect.anchoredPosition3D = state.slotPosition;
            }
            for (int i = 0; i < state.oldIcons.Length; i++)
                if (state.oldIcons[i] != null) state.oldIcons[i].enabled = state.iconEnabled[i];
            if (state.group == null) continue;
            state.group.alpha = state.groupAlpha;
            state.group.interactable = state.groupInteractable;
            state.group.blocksRaycasts = state.groupBlocks;
        }
    }

    private void OnDestroy()
    {
        if (!prepared) return;
        if (wheelArea != null) Destroy(wheelArea.gameObject);
        foreach (var state in states)
            if (state.ownsGroup && state.group != null) Destroy(state.group);
    }

    private void OnValidate()
    {
        visibleRows = Mathf.Max(1, visibleRows);
        cameraDistance = Mathf.Max(0.1f, cameraDistance);
        topScale = Mathf.Max(0.05f, topScale);
        bottomScale = Mathf.Max(0.05f, bottomScale);
        recyclePadding = Mathf.Max(0, recyclePadding);
        hitPadding = Vector2.Max(Vector2.zero, hitPadding);
        smoothResponse = Mathf.Max(0.1f, smoothResponse);
        maxRowsPerSecond = Mathf.Max(0.1f, maxRowsPerSecond);
    }
}
