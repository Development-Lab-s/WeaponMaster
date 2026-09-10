using UnityEngine;
using UnityEngine.UI;


public class InventoryDragController : MonoBehaviour
{
    private static InventoryDragController s_instance;

    public static InventoryDragController Instance => s_instance;


    public InventorySlotUI SourceSlot { get; private set; }
    public bool IsDragging => SourceSlot != null;

    private RectTransform _root;
    private RectTransform _ghost;
    private Image _ghostIcon;
    private Canvas _canvas;
    
    public static InventoryDragController GetOrCreate(Canvas canvas)
    {
        if (s_instance != null) return s_instance;
        if (canvas == null) return null;

        var go = new GameObject("InventoryDragLayer", typeof(RectTransform), typeof(InventoryDragController));
        var rt = (RectTransform)go.transform;
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.SetAsLastSibling();

        CanvasGroup cg = go.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;   // 뭔가 냅둬야 하는 특별한 이유가 있었는데 까먹어서 걍 냅두기로함
        cg.interactable = false;

        s_instance = go.GetComponent<InventoryDragController>();
        s_instance.Init(canvas, rt);
        return s_instance;
    }

    private void Init(Canvas canvas, RectTransform root)
    {
        _canvas = canvas;
        _root = root;
        BuildGhost();
    }

    private void OnDestroy()
    {
        if (s_instance == this) s_instance = null;
    }

    private void BuildGhost()
    {
        var go = new GameObject("Ghost", typeof(RectTransform), typeof(Image));
        _ghost = (RectTransform)go.transform;
        _ghost.SetParent(_root, false);
        _ghost.sizeDelta = new Vector2(72, 72);

        _ghostIcon = go.GetComponent<Image>();
        _ghostIcon.raycastTarget = false;
        _ghostIcon.preserveAspect = true;
        _ghostIcon.color = new Color(1, 1, 1, 0.85f);

        _ghost.gameObject.SetActive(false);
    }

    public void BeginDrag(InventorySlotUI slot, Vector2 screenPosition, Camera eventCamera)
    {
        if (slot == null) return;
        var item = slot.CurrentItem;
        if (item == null) return;

        SourceSlot = slot;

        _ghost.sizeDelta = slot.CellSize * 0.9f;
        _ghostIcon.sprite = item.icon;
        _ghost.gameObject.SetActive(true);
        _ghost.SetAsLastSibling();
        UpdatePosition(screenPosition, eventCamera);

        slot.SetDragSourceVisual(true);
    }

    public void UpdatePosition(Vector2 screenPosition, Camera eventCamera)
    {
        if (!IsDragging) return;

        Camera cam = _canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : eventCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screenPosition, cam, out var local))
            _ghost.localPosition = local;
    }

    public void EndDrag()
    {
        if (SourceSlot != null) SourceSlot.SetDragSourceVisual(false);
        SourceSlot = null;
        if (_ghost != null) _ghost.gameObject.SetActive(false);
    }
}
