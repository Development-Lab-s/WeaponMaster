using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[DefaultExecutionOrder(200)]
public sealed class BKWeaponScroll : MonoBehaviour, IScrollHandler 
{
    [Serializable]
    public class WeaponEntry
    {
        public Transform visualRoot;
        public InventorySlotUI dragSlot;
        [Tooltip("선택 사항: 원통 안의 실제 연결 지점. 비우면 원통의 긴 축에서 자동 계산합니다.")]
        public Transform weaponSocket;
        [Tooltip("연결 지점 기준 보정. 무기 종류별 보정은 아래 Weapon Styles에서 합니다.")]
        public Vector2 weaponOffset;
        [Min(0.01f)] public float weaponImageScale = 1f;
    }

    [Serializable]
    public class WeaponStyle
    {
        public Texture texture;
        [HideInInspector] public float gripAlongWeapon = 0.82f;
        [Tooltip("원본 텍스처의 손잡이 좌표를 직접 정할 때만 켭니다.")]
        public bool overrideGripUV;
        public Vector2 gripUV = new Vector2(0.8f, 0.5f);
        public bool overrideSourceAngle;
        public float sourceAngle;
        [Min(0.01f)] public float lengthScale = 1f;
        public Vector2 offset;
        public float angleOffset;
    }

    [Header("기존 연결 유지")]
    [SerializeField] private Camera previewCamera;
    [SerializeField] private RawImage previewImage;
    [SerializeField] private WeaponEntry[] items;

    [Header("목록 크기 (기존 수치에 List Scale을 곱합니다)")]
    [Min(0.1f)] [SerializeField] private float listScale = 1.55f;
    [SerializeField] private Vector2 viewportSize = new Vector2(720f, 560f);
    [SerializeField] private float rightPadding = 0f;
    [SerializeField] private float verticalOffset = 0f;
    [Min(1f)] [SerializeField] private float rowSpacing = 200f;
    [Min(10f)] [SerializeField] private float barrelHeight = 140f;
    [Min(0f)] [SerializeField] private float barrelRightInset = 20f;

    [Header("통과 무기의 연결")]
    [Min(20f)] [SerializeField] private float weaponLength = 340f;
    [Range(0f, 1f)] [SerializeField] private float socketAlongBarrel = 0.2f;
    [HideInInspector] [SerializeField] private float gripAlongWeapon = 0.82f;
    [Tooltip("원본 촬영 이미지 전체의 폭입니다. Weapon Length에 이 값을 곱합니다.")]
    [Min(0.1f)] [SerializeField] private float textureFrameScale = 1f;
    [SerializeField] private WeaponStyle[] weaponStyles = new WeaponStyle[0];

    [Header("크기 / 스크롤")]
    [Min(0.05f)] [SerializeField] private float topScale = 0.7f;
    [Min(0.05f)] [SerializeField] private float bottomScale = 1.3f;
    [Tooltip("휠 입력 한 번의 이동량(칸 단위). 새 입력은 현재 위치에서 이 거리만큼 이동하도록 갱신합니다.")]
    [SerializeField] private float wheelStep = 1f;
    [Tooltip("마지막 입력 후 이동을 마무리하는 시간(초). 0.1 권장. 미처리 이동은 누적하지 않습니다.")]
    [Range(0.03f, 0.2f)] [SerializeField] private float smoothDuration = 0.1f;
    [Min(0.1f)] [SerializeField] private float cameraDistance = 10f;

    private sealed class RectBackup
    {
        public readonly RectTransform rect;
        private readonly Vector2 min, max, pivot, size;
        private readonly Vector3 position, scale;
        private readonly Quaternion rotation;

        public RectBackup(RectTransform value)
        {
            rect = value;
            min = value.anchorMin; max = value.anchorMax;
            pivot = value.pivot; size = value.sizeDelta;
            position = value.anchoredPosition3D;
            scale = value.localScale; rotation = value.localRotation;
        }

        public void Restore()
        {
            if (!rect) return;
            // 비활성화 중에는 좌표만 복원합니다. 계층은 변경하지 않습니다.
            rect.anchorMin = min; rect.anchorMax = max;
            rect.pivot = pivot; rect.sizeDelta = size;
            rect.localRotation = rotation; rect.localScale = scale;
            rect.anchoredPosition3D = position;
        }
    }

    private sealed class MaskState
    {
        public RectMask2D mask;
        public bool created, enabled;
        public Vector4 padding;
        public Vector2Int softness;

        public MaskState(RectTransform target)
        {
            mask = target.GetComponent<RectMask2D>();
            created = !mask;
            if (!mask) mask = target.gameObject.AddComponent<RectMask2D>();
            enabled = mask.enabled; padding = mask.padding; softness = mask.softness;
        }
        public void Activate()
        {
            mask.padding = Vector4.zero; mask.softness = Vector2Int.zero;
            mask.enabled = true;
        }
        public void Restore()
        {
            if (!mask) return;
            mask.padding = padding; mask.softness = softness;
            mask.enabled = !created && enabled;
        }
        public void Dispose()
        {
            if (created && mask) UnityEngine.Object.Destroy(mask);
        }
    }

    private sealed class RowState
    {
        public WeaponEntry entry;
        public RectBackup slot, iconRect;
        public Image hit;
        public bool hitRaycast, iconEnabled, iconRaycast;
        public RawImage icon;
        public Rect originalUV;
        public MaskState mask;
        public readonly List<DisplayCopy> copies = new List<DisplayCopy>();
        public Renderer[] renderers;
        public Quaternion rotation;
        public Vector3 position, scale, localCenter, axisA, axisB;
        public float worldWidth, worldHeight;
    }

    private sealed class SourceView
    {
        public Camera camera;
        public Transform model;
        public Bounds modelBounds;
        public bool hasBounds;
    }

    private sealed class DisplayCopy
    {
        public Transform root;
        public RectTransform slot;
        public RawImage icon;
        public Image hit;
        public Renderer[] renderers;
        public bool generated;
        public readonly List<Mesh> ownedMeshes = new List<Mesh>();

        public void Show(bool value)
        {
            foreach (Renderer renderer in renderers) if (renderer) renderer.enabled = value;
            if (icon) icon.enabled = value && icon.texture;
            if (hit) hit.raycastTarget = value;
        }
    }

    private struct IconPose
    {
        public Vector2 gripUV, frameSize;
        public Vector2 correction;
        public float sourceAngle, angleOffset;
    }

    private struct Geometry
    {
        public Rect uvBounds;
        public Vector2 axisA, axisB;
    }

    private RectTransform owner, grid;
    private RectBackup gridBackup, previewBackup;
    private MaskState gridMask;
    private Image gridInput;
    private bool createdGridInput, gridInputEnabled, gridInputRaycast;
    private Color gridInputColor;
    private RowState[] rows;
    private bool prepared, live, previewRaycast;
    private float offset;
    private readonly BKWeaponWheelMotion wheelMotion = new BKWeaponWheelMotion();
    private readonly Dictionary<Texture, SourceView> sources = new Dictionary<Texture, SourceView>();
    private readonly HashSet<Texture> warnedTextures = new HashSet<Texture>();

    private bool Fail(string message)
    {
        Debug.LogError("[BKWeaponScroll V3.2] " + message, this);
        return false;
    }

    private void Start()
    {
        Canvas.ForceUpdateCanvases();
        if (!Prepare()) { enabled = false; return; }
        prepared = true;
        Activate();
    }

    // 재활성화 시 배치는 LateUpdate에서 수행합니다.
    private void LateUpdate()
    {
        if (!prepared) return;
        if (!live) Activate();
        if (!live) return;
        if (Dragging()) { wheelMotion.Cancel(); return; }
        if (items == null || items.Length != rows.Length || !ValidLayout())
        {
            enabled = false;
            return;
        }

        // 마지막 입력 후 짧은 시간 안에 끝납니다. 오래된 목표를 계속 따라가지 않습니다.
        offset = Mathf.Repeat(offset + wheelMotion.Advance(Time.unscaledDeltaTime), rows.Length);
        Layout();
    }

    private bool Prepare()
    {
        owner = (RectTransform)transform;
        if (!previewCamera || !previewImage || items == null || items.Length < 2)
            return Fail("Preview Camera / Preview Image / Items를 확인하세요.");
        if (!previewCamera.orthographic || !previewCamera.targetTexture ||
            previewImage.texture != previewCamera.targetTexture)
            return Fail("Orthographic 카메라와 Preview Image에 같은 Render Texture가 필요합니다.");
        if (previewImage.uvRect.width <= 0f || previewImage.uvRect.height <= 0f ||
            cameraDistance <= previewCamera.nearClipPlane ||
            cameraDistance >= previewCamera.farClipPlane)
            return Fail("Preview Image의 UV 크기와 Camera Distance를 확인하세요.");
        if (!ValidLayout()) return false;

        var roots = new HashSet<Transform>();
        var slots = new HashSet<InventorySlotUI>();
        rows = new RowState[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            WeaponEntry entry = items[i];
            if (entry == null || !entry.visualRoot || !entry.dragSlot)
                return Fail("Items " + i + "의 연결이 비어 있습니다.");
            if (!roots.Add(entry.visualRoot) || !slots.Add(entry.dragSlot))
                return Fail("WeaponRow / WeaponSlot을 중복 연결하지 마세요.");
            if (entry.visualRoot is RectTransform ||
                previewCamera.transform.IsChildOf(entry.visualRoot))
                return Fail("Visual Root에는 3D WeaponRow를 연결하세요.");
            if (entry.weaponSocket && !entry.weaponSocket.IsChildOf(entry.visualRoot))
                return Fail("Weapon Socket은 해당 WeaponRow 안에 있어야 합니다.");
            for (int j = 0; j < i; j++)
                if (entry.visualRoot.IsChildOf(items[j].visualRoot) ||
                    items[j].visualRoot.IsChildOf(entry.visualRoot))
                    return Fail("WeaponRow들은 서로의 자식이 아니어야 합니다.");
            foreach (Transform child in entry.visualRoot)
                if (child.gameObject.activeSelf &&
                    child.GetComponentsInChildren<Camera>(true).Length > 0)
                    return Fail(entry.visualRoot.name + "의 복제 촬영 묶음 '" + child.name +
                                "'을 꺼주세요. 원통은 켜둡니다.");

            RectTransform slot = entry.dragSlot.transform as RectTransform;
            if (!slot || !slot.IsChildOf(owner) || slot.parent == owner)
                return Fail("슬롯은 WeaponListPanel 아래 Grid 안에 있어야 합니다.");
            if (i == 0) grid = slot.parent as RectTransform;
            if (!grid || slot.parent != grid || grid.parent != owner)
                return Fail("기존 Grid와 슬롯의 계층을 유지하세요.");
            RawImage[] icons = slot.GetComponentsInChildren<RawImage>(true);
            if (icons.Length != 1 || icons[0].transform.parent != slot)
                return Fail(entry.dragSlot.name + " 바로 아래에 무기 RawImage 하나가 필요합니다.");
            // 자식 GameObject의 활성화 상태도 이 스크립트에서 바꾸지 않습니다.
            if (!icons[0].gameObject.activeSelf)
                return Fail(entry.dragSlot.name + "/RawImage 오브젝트를 먼저 켜주세요.");
            Image hit = slot.GetComponent<Image>();
            if (!hit || !hit.enabled || !hit.raycastTarget || !slot.gameObject.activeInHierarchy)
                return Fail(entry.dragSlot.name + "의 입력용 Image와 Raycast Target을 켜주세요.");

            Geometry geometry;
            if (!Measure(previewCamera, entry.visualRoot.GetComponentsInChildren<Renderer>(true),
                         out geometry))
                return Fail(entry.visualRoot.name + "에서 카메라가 찍는 원통을 찾지 못했습니다.");
            float depth = previewCamera.WorldToViewportPoint(entry.visualRoot.position).z;
            Vector3 center = previewCamera.ViewportToWorldPoint(
                new Vector3(geometry.uvBounds.center.x, geometry.uvBounds.center.y, depth));
            Vector3 a = previewCamera.ViewportToWorldPoint(
                new Vector3(geometry.axisA.x, geometry.axisA.y, depth));
            Vector3 b = previewCamera.ViewportToWorldPoint(
                new Vector3(geometry.axisB.x, geometry.axisB.y, depth));
            rows[i] = new RowState
            {
                entry = entry, slot = new RectBackup(slot), iconRect = new RectBackup(icons[0].rectTransform),
                hit = hit, hitRaycast = hit.raycastTarget, icon = icons[0],
                iconEnabled = icons[0].enabled, iconRaycast = icons[0].raycastTarget,
                originalUV = icons[0].uvRect,
                position = entry.visualRoot.localPosition, scale = entry.visualRoot.localScale,
                rotation = entry.visualRoot.localRotation,
                renderers = VisibleRenderers(entry.visualRoot, previewCamera),
                localCenter = entry.visualRoot.InverseTransformPoint(center),
                axisA = entry.visualRoot.InverseTransformPoint(a),
                axisB = entry.visualRoot.InverseTransformPoint(b),
                worldHeight = geometry.uvBounds.height * 2f * previewCamera.orthographicSize,
                worldWidth = geometry.uvBounds.width * 2f * previewCamera.orthographicSize * previewCamera.aspect
            };
        }

        if (previewImage.transform.parent != grid ||
            previewImage.transform.GetSiblingIndex() < items.Length)
            return Fail("ObjImage를 Grid의 기존 슬롯들 뒤에 두세요. 슬롯 순서는 유지합니다.");
        LayoutGroup layout = grid.GetComponent<LayoutGroup>();
        if (layout && layout.enabled)
            return Fail("Grid의 Layout Group을 꺼주세요.");
        gridBackup = new RectBackup(grid);
        previewBackup = new RectBackup(previewImage.rectTransform);
        previewRaycast = previewImage.raycastTarget;

        // 현재 장면의 원래 촬영 카메라를 읽기만 합니다. 촬영 설정/공유 RT는 변경하지 않습니다.
        foreach (Camera camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (!camera.isActiveAndEnabled || camera == previewCamera || !camera.targetTexture ||
                !camera.transform.parent) continue;
            Texture texture = camera.targetTexture;
            if (sources.ContainsKey(texture))
            {
                sources[texture] = null;
                continue;
            }
            Transform model = FindCaptureModel(camera);
            if (!model) continue;
            Bounds bounds;
            bool hasBounds = ModelBounds(model, VisibleRenderers(model, camera), out bounds);
            sources.Add(texture, new SourceView
            {
                camera = camera, model = model, modelBounds = bounds, hasBounds = hasBounds
            });
        }
        return true;
    }

    private bool ValidLayout()
    {
        if (listScale <= 0f || viewportSize.x <= 0f || viewportSize.y <= 0f ||
            rowSpacing <= 0f || barrelHeight <= 0f || weaponLength <= 0f ||
            topScale <= 0f || bottomScale <= 0f)
            return Fail("크기, 간격과 Scale은 양수여야 합니다.");
        if (items == null || items.Length < 2 || textureFrameScale <= 0f)
            return Fail("Items는 2개 이상, Texture Frame Scale은 양수여야 합니다.");
        return true;
    }

    private void Activate()
    {
        if (!ValidLayout()) { enabled = false; return; }
        if (gridMask == null) gridMask = new MaskState(grid);
        if (!gridInput)
        {
            gridInput = grid.GetComponent<Image>();
            createdGridInput = !gridInput;
            if (!gridInput) gridInput = grid.gameObject.AddComponent<Image>();
            gridInputEnabled = gridInput.enabled; gridInputRaycast = gridInput.raycastTarget;
            gridInputColor = gridInput.color;
        }
        gridInput.enabled = true;
        gridInput.raycastTarget = true;
        if (createdGridInput) gridInput.color = Color.clear;
        gridMask.Activate();
        previewImage.raycastTarget = false;
        foreach (RowState row in rows)
        {
            // 칸별로 자르지 않습니다. 기존 마스크가 있으면 실행 중에만 끕니다.
            if (row.mask == null && row.slot.rect.GetComponent<RectMask2D>())
                row.mask = new MaskState(row.slot.rect);
            if (row.mask != null) row.mask.mask.enabled = false;
            row.icon.enabled = true;
            row.icon.raycastTarget = false;
        }
        live = true;
        Layout();
    }

    private static bool Dragging()
    {
        return InventoryDragController.Instance != null && InventoryDragController.Instance.IsDragging;
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (!isActiveAndEnabled || !live || Dragging() ||
            Mathf.Approximately(eventData.scrollDelta.y, 0f)) return;
        // 남은 거리에 더하지 않고 현재 표시 위치를 기준으로 새 구간을 시작합니다.
        wheelMotion.Begin(-Mathf.Sign(eventData.scrollDelta.y) * wheelStep, smoothDuration);
        eventData.Use();
    }

    private void Layout()
    {
        Vector2 size = viewportSize * listScale;
        float pitch = rowSpacing * listScale;
        float cycle = rows.Length * pitch;
        grid.anchorMin = grid.anchorMax = new Vector2(1f, 0.5f);
        grid.pivot = new Vector2(1f, 0.5f);
        grid.localScale = Vector3.one;
        grid.localRotation = Quaternion.identity;
        grid.anchoredPosition3D = new Vector3(-rightPadding, verticalOffset, 0f);
        grid.sizeDelta = size;

        RectTransform imageRect = previewImage.rectTransform;
        Rect uv = previewImage.uvRect;
        float aspect = previewCamera.aspect * uv.width / uv.height;
        // 겹친 통도 원래 비율로 찍고, 목록 바깥쪽에서만 잘라냅니다.
        float imageHeight = Mathf.Max(size.y, size.x / aspect);
        imageRect.anchorMin = imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition3D = Vector3.zero;
        imageRect.localRotation = Quaternion.identity;
        imageRect.localScale = Vector3.one;
        imageRect.sizeDelta = new Vector2(imageHeight * aspect, imageHeight);
        float pixelsY = imageHeight / (2f * previewCamera.orthographicSize * uv.height);
        float pixelsX = imageRect.rect.width /
            (2f * previewCamera.orthographicSize * previewCamera.aspect * uv.width);

        for (int i = 0; i < rows.Length; i++)
        {
            RowState row = rows[i];
            if (!row.entry.visualRoot || !row.slot.rect || !row.icon)
            {
                Fail("실행 중 슬롯이 사라졌습니다. Play를 종료하고 연결을 확인하세요.");
                enabled = false; return;
            }
            IconPose pose = GetIconPose(row);
            float maxZoom = Mathf.Max(topScale, bottomScale);
            // 실제 그림보다 큰 여유를 두어 복사본을 화면 밖에서 재사용합니다.
            float reachX = pose.frameSize.x * Mathf.Max(Mathf.Abs(pose.gripUV.x), Mathf.Abs(1f - pose.gripUV.x));
            float reachY = pose.frameSize.y * Mathf.Max(Mathf.Abs(pose.gripUV.y), Mathf.Abs(1f - pose.gripUV.y));
            float barrelWidth = row.worldWidth / row.worldHeight * barrelHeight * listScale;
            float reach = Mathf.Sqrt(reachX * reachX + reachY * reachY);
            float margin = maxZoom * (reach + pose.correction.magnitude +
                           barrelWidth + barrelHeight * listScale) + pitch;
            float baseTop = (i + 0.5f - offset) * pitch;
            int first = BKWeaponListLayout.FirstRepeat(baseTop, cycle, margin);
            int last = BKWeaponListLayout.LastRepeat(baseTop, cycle, size.y, margin);
            int count = Mathf.Max(0, last - first + 1);
            EnsureCopies(row, count);

            for (int copyIndex = 0; copyIndex < row.copies.Count; copyIndex++)
            {
                DisplayCopy copy = row.copies[copyIndex];
                if (copyIndex >= count) { copy.Show(false); continue; }
                if (copy.generated) copy.icon.texture = row.icon.texture;
                copy.Show(true);
                float fromTop = baseTop + (first + copyIndex) * cycle;
                float zoom = Mathf.Lerp(topScale, bottomScale,
                    Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(fromTop / size.y)));
                float modelScale = barrelHeight * listScale * zoom / (row.worldHeight * pixelsY);
                float width = row.worldWidth * pixelsX * modelScale;
                Vector2 center = new Vector2(
                    grid.rect.xMax - barrelRightInset * listScale - width * 0.5f,
                    grid.rect.yMax - fromTop);
                Transform root = copy.root;
                root.localScale = row.scale * modelScale;
                Vector3 imagePoint = imageRect.InverseTransformPoint(grid.TransformPoint(center));
                // 겹치는 통 표면이 같은 깊이에서 깜빡이지 않도록 아래쪽을 조금 앞에 둡니다.
                float depth = Mathf.Clamp(cameraDistance - Mathf.Clamp01(fromTop / size.y) * 0.02f,
                    previewCamera.nearClipPlane + 0.01f, previewCamera.farClipPlane - 0.01f);
                Vector3 world = previewCamera.ViewportToWorldPoint(new Vector3(
                    uv.x + (imagePoint.x - imageRect.rect.xMin) / imageRect.rect.width * uv.width,
                    uv.y + (imagePoint.y - imageRect.rect.yMin) / imageRect.rect.height * uv.height,
                    depth));
                root.position = world - root.TransformVector(row.localCenter);

                RectTransform slot = copy.slot;
                slot.anchorMin = slot.anchorMax = new Vector2(0.5f, 0.5f);
                slot.pivot = new Vector2(0.5f, 0.5f);
                slot.localRotation = Quaternion.identity;
                slot.localScale = Vector3.one;
                // 클릭 영역은 칸 중심 사이에서 나눕니다. 그림 크기는 줄이지 않습니다.
                slot.sizeDelta = new Vector2(size.x, pitch);
                slot.anchoredPosition3D = new Vector3(0f, center.y, 0f);
                copy.hit.raycastTarget = row.hitRaycast &&
                    fromTop + pitch * 0.5f > 0f && fromTop - pitch * 0.5f < size.y;

                Vector2 a = ProjectToSlot(root.TransformPoint(row.axisA), slot);
                Vector2 b = ProjectToSlot(root.TransformPoint(row.axisB), slot);
                Vector3 localSocket = row.entry.weaponSocket
                    ? row.entry.visualRoot.InverseTransformPoint(row.entry.weaponSocket.position)
                    : Vector3.Lerp(row.axisA, row.axisB, socketAlongBarrel);
                Vector2 socket = ProjectToSlot(root.TransformPoint(localSocket), slot);
                float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
                PlaceIcon(copy, pose, socket, angle, zoom);
            }
        }
    }

    private Vector2 ProjectToSlot(Vector3 world, RectTransform slot)
    {
        Vector3 p = previewCamera.WorldToViewportPoint(world);
        Rect uv = previewImage.uvRect;
        RectTransform rt = previewImage.rectTransform;
        Vector3 imageLocal = new Vector3(
            rt.rect.xMin + (p.x - uv.x) / uv.width * rt.rect.width,
            rt.rect.yMin + (p.y - uv.y) / uv.height * rt.rect.height, 0f);
        return slot.InverseTransformPoint(rt.TransformPoint(imageLocal));
    }

    private IconPose GetIconPose(RowState row)
    {
        Texture texture = row.icon.texture;
        WeaponStyle style = null;
        if (texture && weaponStyles != null)
            foreach (WeaponStyle candidate in weaponStyles)
                if (candidate != null && candidate.texture == texture) { style = candidate; break; }
        float lengthScale = style != null && style.lengthScale > 0f ? style.lengthScale : 1f;
        float rowScale = row.entry.weaponImageScale > 0f ? row.entry.weaponImageScale : 1f;
        float width = weaponLength * textureFrameScale * listScale * lengthScale * rowScale;
        IconPose pose = new IconPose
        {
            gripUV = new Vector2(Mathf.Clamp01(style != null ? style.gripAlongWeapon : gripAlongWeapon), 0.5f),
            frameSize = new Vector2(width, texture ? width * texture.height / texture.width : width),
            correction = (row.entry.weaponOffset + (style != null ? style.offset : Vector2.zero)) * listScale,
            angleOffset = style != null ? style.angleOffset : 0f,
            sourceAngle = 0f
        };
        SourceView source;
        if (texture && sources.TryGetValue(texture, out source) && source != null &&
            source.camera && source.model)
        {
            // 사슬의 길이나 렌더러 중심 대신 실제 무기 모델의 원점을 손잡이 기준으로 씁니다.
            Vector3 p = source.camera.WorldToViewportPoint(source.model.position);
            if (p.z > source.camera.nearClipPlane && p.z < source.camera.farClipPlane &&
                p.x > -0.5f && p.x < 1.5f && p.y > -0.5f && p.y < 1.5f)
                pose.gripUV = p;
            if (source.hasBounds)
                pose.sourceAngle = ProjectModelAngle(source, texture);
        }
        else if (texture && warnedTextures.Add(texture))
            Debug.LogWarning("[BKWeaponScroll V3.2] " + texture.name +
                "의 원래 촬영 카메라/모델을 찾지 못했습니다. Weapon Styles의 Grip UV와 Source Angle로 보정할 수 있습니다.", this);

        if (style != null)
        {
            if (style.overrideGripUV) pose.gripUV = style.gripUV;
            if (style.overrideSourceAngle) pose.sourceAngle = style.sourceAngle;
        }
        return pose;
    }

    private static void PlaceIcon(DisplayCopy copy, IconPose pose, Vector2 socket, float barrelAngle, float zoom)
    {
        if (!copy.icon.texture) { copy.icon.enabled = false; return; }
        RectTransform icon = copy.icon.rectTransform;
        icon.anchorMin = icon.anchorMax = new Vector2(0.5f, 0.5f);
        icon.pivot = pose.gripUV;
        icon.sizeDelta = pose.frameSize * zoom;
        icon.localScale = Vector3.one;
        icon.localRotation = Quaternion.Euler(0f, 0f, barrelAngle - pose.sourceAngle + pose.angleOffset);
        Vector2 position = socket + (Vector2)(Quaternion.Euler(0f, 0f, barrelAngle) *
                                             (Vector3)(pose.correction * zoom));
        icon.anchoredPosition3D = new Vector3(position.x, position.y, 0f);
        // 철퇴 머리/사슬/손잡이를 모두 표시합니다. 자동 UV 자르기와 칸 맞춤 축소는 하지 않습니다.
        copy.icon.uvRect = new Rect(0f, 0f, 1f, 1f);
    }

    private static Transform FindCaptureModel(Camera camera)
    {
        Transform result = null;
        foreach (Transform child in camera.transform.parent)
        {
            if (!child.gameObject.activeInHierarchy || child.GetComponent<Camera>() ||
                child.GetComponentsInChildren<Renderer>(false).Length == 0) continue;
            if (result) return null;
            result = child;
        }
        return result;
    }

    private static Renderer[] VisibleRenderers(Transform root, Camera camera)
    {
        var result = new List<Renderer>();
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            if (renderer && renderer.enabled && renderer.gameObject.activeInHierarchy &&
                (camera.cullingMask & (1 << renderer.gameObject.layer)) != 0)
                result.Add(renderer);
        return result.ToArray();
    }

    private static bool ModelBounds(Transform model, Renderer[] renderers, out Bounds result)
    {
        result = default;
        bool found = false;
        foreach (Renderer renderer in renderers)
        {
            Bounds bounds = renderer.localBounds;
            // 철퇴의 저장된 SkinnedMesh 경계가 0이어도 실제 스킨 메시에서 경계를 얻습니다.
            if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh)
            {
                Mesh mesh = new Mesh();
                try
                {
                    skinned.BakeMesh(mesh);
                    mesh.RecalculateBounds();
                    bounds = mesh.bounds;
                }
                finally { Destroy(mesh); }
            }
            if (bounds.size.sqrMagnitude < 0.000001f) continue;
            for (int i = 0; i < 8; i++)
            {
                Vector3 sign = new Vector3((i & 1) == 0 ? -1f : 1f,
                                          (i & 2) == 0 ? -1f : 1f,
                                          (i & 4) == 0 ? -1f : 1f);
                Vector3 p = model.InverseTransformPoint(renderer.transform.TransformPoint(
                    bounds.center + Vector3.Scale(bounds.extents, sign)));
                if (found) result.Encapsulate(p);
                else { result = new Bounds(p, Vector3.zero); found = true; }
            }
        }
        return found;
    }

    private static float ProjectModelAngle(SourceView source, Texture texture)
    {
        float best = 0f, angle = 0f;
        for (int axis = 0; axis < 3; axis++)
        {
            Vector3 delta = Vector3.zero;
            delta[axis] = source.modelBounds.extents[axis];
            Vector3 a = source.camera.WorldToViewportPoint(source.model.TransformPoint(source.modelBounds.center - delta));
            Vector3 b = source.camera.WorldToViewportPoint(source.model.TransformPoint(source.modelBounds.center + delta));
            if (a.z <= source.camera.nearClipPlane || b.z <= source.camera.nearClipPlane) continue;
            Vector2 direction = new Vector2((b.x - a.x) * texture.width, (b.y - a.y) * texture.height);
            if (direction.sqrMagnitude <= best) continue;
            best = direction.sqrMagnitude;
            if (direction.x < 0f) direction = -direction;
            angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }
        return angle;
    }

    private void EnsureCopies(RowState row, int count)
    {
        if (row.copies.Count == 0)
            row.copies.Add(new DisplayCopy
            {
                root = row.entry.visualRoot, slot = row.slot.rect, icon = row.icon,
                hit = row.hit, renderers = row.renderers, generated = false
            });
        while (row.copies.Count < count)
        {
            DisplayCopy copy = new DisplayCopy { generated = true };
            // Transform/MeshRenderer만 새로 만듭니다. 무기/네트워크/물리 스크립트는 복제하지 않습니다.
            var rootObject = new GameObject("BK Barrels " + row.entry.visualRoot.name + " " + row.copies.Count);
            rootObject.layer = row.entry.visualRoot.gameObject.layer;
            copy.root = rootObject.transform;
            copy.root.SetParent(row.entry.visualRoot.parent, false);
            copy.root.localPosition = row.position;
            copy.root.localRotation = row.rotation;
            copy.root.localScale = row.scale;
            var transforms = new Dictionary<Transform, Transform> { { row.entry.visualRoot, copy.root } };
            var renderers = new List<Renderer>();
            foreach (Renderer original in row.renderers)
            {
                Transform target = CopyTransform(original.transform, row.entry.visualRoot, transforms);
                Mesh mesh = null;
                MeshFilter sourceFilter = original.GetComponent<MeshFilter>();
                if (sourceFilter) mesh = sourceFilter.sharedMesh;
                else if (original is SkinnedMeshRenderer skin && skin.sharedMesh)
                {
                    mesh = new Mesh();
                    skin.BakeMesh(mesh);
                    copy.ownedMeshes.Add(mesh);
                }
                if (!mesh) continue;
                target.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = target.gameObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = original.sharedMaterials;
                renderer.shadowCastingMode = original.shadowCastingMode;
                renderer.receiveShadows = original.receiveShadows;
                renderer.lightProbeUsage = original.lightProbeUsage;
                renderer.reflectionProbeUsage = original.reflectionProbeUsage;
                renderer.renderingLayerMask = original.renderingLayerMask;
                var properties = new MaterialPropertyBlock();
                original.GetPropertyBlock(properties);
                renderer.SetPropertyBlock(properties);
                renderers.Add(renderer);
            }
            copy.renderers = renderers.ToArray();

            var uiObject = new GameObject("BK Display " + row.entry.dragSlot.name + " " + row.copies.Count,
                typeof(RectTransform), typeof(Image), typeof(BKWeaponScrollSlotProxy));
            copy.slot = (RectTransform)uiObject.transform;
            copy.slot.SetParent(grid, false);
            copy.slot.SetSiblingIndex(previewImage.transform.GetSiblingIndex());
            copy.hit = uiObject.GetComponent<Image>();
            copy.hit.color = Color.clear;
            uiObject.GetComponent<BKWeaponScrollSlotProxy>().source = row.entry.dragSlot;
            var iconObject = new GameObject("Weapon Image", typeof(RectTransform), typeof(RawImage));
            iconObject.transform.SetParent(copy.slot, false);
            copy.icon = iconObject.GetComponent<RawImage>();
            copy.icon.material = row.icon.material;
            copy.icon.color = row.icon.color;
            copy.icon.raycastTarget = false;
            row.copies.Add(copy);
        }
    }

    private static Transform CopyTransform(Transform source, Transform sourceRoot,
                                          Dictionary<Transform, Transform> copies)
    {
        Transform result;
        if (copies.TryGetValue(source, out result)) return result;
        Transform parent = CopyTransform(source.parent, sourceRoot, copies);
        var go = new GameObject(source.name);
        go.layer = source.gameObject.layer;
        result = go.transform;
        result.SetParent(parent, false);
        result.localPosition = source.localPosition;
        result.localRotation = source.localRotation;
        result.localScale = source.localScale;
        copies.Add(source, result);
        return result;
    }

    // 모델의 로컬 경계를 투영해 촬영 이미지의 여백을 줄입니다.
    private static bool Measure(Camera camera, Renderer[] renderers, out Geometry result)
    {
        result = default;
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        float longest = 0f;
        bool found = false;
        foreach (Renderer renderer in renderers)
        {
            if (!renderer || !renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                (camera.cullingMask & (1 << renderer.gameObject.layer)) == 0) continue;
            Bounds bounds = renderer.localBounds;
            Vector2 partMin = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 partMax = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            bool inFront = true;
            for (int i = 0; i < 8; i++)
            {
                Vector3 sign = new Vector3((i & 1) == 0 ? -1f : 1f,
                                          (i & 2) == 0 ? -1f : 1f,
                                          (i & 4) == 0 ? -1f : 1f);
                Vector3 p = camera.WorldToViewportPoint(renderer.transform.TransformPoint(
                    bounds.center + Vector3.Scale(bounds.extents, sign)));
                if (p.z <= camera.nearClipPlane || p.z >= camera.farClipPlane) inFront = false;
                partMin = Vector2.Min(partMin, p); partMax = Vector2.Max(partMax, p);
            }
            if (!inFront) continue;
            found = true;
            min = Vector2.Min(min, partMin); max = Vector2.Max(max, partMax);
            for (int axis = 0; axis < 3; axis++)
            {
                Vector3 delta = Vector3.zero;
                delta[axis] = bounds.extents[axis];
                Vector2 a = camera.WorldToViewportPoint(
                    renderer.transform.TransformPoint(bounds.center - delta));
                Vector2 b = camera.WorldToViewportPoint(
                    renderer.transform.TransformPoint(bounds.center + delta));
                Vector2 d = b - a;
                float length = d.x * d.x * camera.aspect * camera.aspect + d.y * d.y;
                if (length <= longest) continue;
                longest = length;
                if (a.x > b.x) { Vector2 temp = a; a = b; b = temp; }
                result.axisA = a; result.axisB = b;
            }
        }
        if (!found || longest < 0.0000001f || max.x - min.x < 0.0001f ||
            max.y - min.y < 0.0001f) return false;
        result.uvBounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return true;
    }

    private void OnDisable()
    {
        wheelMotion.Cancel();
        if (!live) return;
        live = false;
        // 부모의 활성화 상태가 바뀌는 중에도 좌표와 그리기 설정만 복원합니다.
        gridBackup.Restore();
        previewBackup.Restore();
        if (previewImage) previewImage.raycastTarget = previewRaycast;
        foreach (RowState row in rows)
        {
            foreach (DisplayCopy copy in row.copies)
                if (copy.generated) copy.Show(false);
            foreach (Renderer renderer in row.renderers)
                if (renderer) renderer.enabled = true;
            if (row.entry.visualRoot)
            {
                row.entry.visualRoot.localPosition = row.position;
                row.entry.visualRoot.localScale = row.scale;
            }
            row.slot.Restore(); row.iconRect.Restore();
            if (row.hit) row.hit.raycastTarget = row.hitRaycast;
            if (row.icon)
            {
                row.icon.enabled = row.iconEnabled;
                row.icon.raycastTarget = row.iconRaycast;
                row.icon.uvRect = row.originalUV;
            }
            row.mask?.Restore();
        }
        gridMask?.Restore();
        if (gridInput)
        {
            gridInput.enabled = !createdGridInput && gridInputEnabled;
            gridInput.raycastTarget = gridInputRaycast; gridInput.color = gridInputColor;
        }
    }

    private void OnDestroy()
    {
        OnDisable();
        if (rows != null)
            foreach (RowState row in rows)
            {
                if (row == null) continue;
                row.mask?.Dispose();
                foreach (DisplayCopy copy in row.copies)
                {
                    if (!copy.generated) continue;
                    if (copy.root) Destroy(copy.root.gameObject);
                    if (copy.slot) Destroy(copy.slot.gameObject);
                    foreach (Mesh mesh in copy.ownedMeshes) if (mesh) Destroy(mesh);
                }
            }
        gridMask?.Dispose();
        if (createdGridInput && gridInput) Destroy(gridInput);
    }
}

// 한 입력의 잔여 이동만 보유합니다. 같은 방향 재입력은 현재 속도를 이어받고,
// 반대 방향 재입력/드래그/비활성화에서는 이전 방향의 움직임을 남기지 않습니다.
internal sealed class BKWeaponWheelMotion
{
    private float distance, duration, elapsed, traveled, startVelocity, velocity;

    public void Begin(float amount, float seconds)
    {
        if (Mathf.Approximately(amount, 0f)) { Cancel(); return; }

        distance = amount;
        duration = Mathf.Clamp(seconds, 0.03f, 0.2f);
        elapsed = 0f;
        traveled = 0f;
        float direction = Mathf.Sign(amount);
        // 이 범위의 시작 속도와 끝 속도 0을 사용하면 목표를 넘어갔다 돌아오지 않습니다.
        startVelocity = direction * Mathf.Clamp(velocity * direction,
            0f, 3f * Mathf.Abs(amount) / duration);
        velocity = startVelocity;
    }

    public float Advance(float deltaTime)
    {
        if (duration <= 0f || deltaTime <= 0f) return 0f;
        elapsed = Mathf.Min(duration, elapsed + deltaTime);
        if (elapsed >= duration)
        {
            float lastMove = distance - traveled;
            Cancel();
            return lastMove;
        }

        float t = elapsed / duration;
        float t2 = t * t;
        float t3 = t2 * t;
        // 현재 속도로 출발하고 끝에서는 속도 0이 되는 Hermite 보간입니다.
        float position = distance * (3f * t2 - 2f * t3) +
                         startVelocity * duration * (t3 - 2f * t2 + t);
        velocity = (distance * (6f * t - 6f * t2) +
                    startVelocity * duration * (3f * t2 - 4f * t + 1f)) / duration;
        float move = position - traveled;
        traveled = position;
        return move;
    }

    public void Cancel()
    {
        distance = duration = elapsed = traveled = startVelocity = velocity = 0f;
    }
}

internal static class BKWeaponListLayout
{
    public static int FirstRepeat(double baseTop, double cycle, double margin)
    {
        return (int)Math.Ceiling((-margin - baseTop) / cycle);
    }

    public static int LastRepeat(double baseTop, double cycle, double height, double margin)
    {
        return (int)Math.Floor((height + margin - baseTop) / cycle);
    }
}

// 추가 표시본도 원래 InventorySlotUI로 드래그/드롭을 전달합니다.
public sealed class BKWeaponScrollSlotProxy : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [NonSerialized] public InventorySlotUI source;
    public void OnBeginDrag(PointerEventData e) { if (source) source.OnBeginDrag(e); }
    public void OnDrag(PointerEventData e) { if (source) source.OnDrag(e); }
    public void OnEndDrag(PointerEventData e) { if (source) source.OnEndDrag(e); }
    public void OnDrop(PointerEventData e) { if (source) source.OnDrop(e); }
    public void OnPointerEnter(PointerEventData e) { if (source) source.OnPointerEnter(e); }
    public void OnPointerExit(PointerEventData e) { if (source) source.OnPointerExit(e); }
}