using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;


public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;

    private Color NormalColor = new Color(1f, 1f, 1f, 0.06f);
    private Color HoverColor = new Color(1f, 1f, 1f, 0.16f);
    private Color DragSourceColor = new Color(1f, 1f, 1f, 0.03f);
    private Color NameColor = new Color(0.92f, 0.94f, 1f);
    private Color EmptyNameColor = new Color(0.55f, 0.6f, 0.7f);

    private ItemContainer _container;
    private int _index = -1;
    private bool _hovered;
    private bool _isDragSource;
    private string _emptyLabel = string.Empty;

    private static readonly bool fucked = true;

    public ItemContainer Container => _container;
    public int Index => _index;
    public ItemData CurrentItem => _container != null ? _container.Get(_index) : null;
    public Vector2 CellSize => ((RectTransform)transform).rect.size;

    public string EmptyLabel
    {
        get => _emptyLabel;
        set { _emptyLabel = value ?? string.Empty; Refresh(); }
    }

    public void Bind(ItemContainer container, int index)
    {
        _container = container;
        _index = index;
        Refresh();
    }

    public void Refresh()
    {
        var item = CurrentItem;

        if (icon != null)
        {
            bool has = item != null;
            icon.enabled = has;
            if (has)
            {
                icon.sprite = item.icon;
                icon.preserveAspect = true;
            }
            ApplyIconAlpha();
        }

        if (nameText != null)
        {
            nameText.text = item != null ? item.displayName : _emptyLabel;
            nameText.color = item != null ? NameColor : EmptyNameColor;
        }

        UpdateBackground();
    }

    private void UpdateBackground()
    {
        if (background == null) return;

        if (_isDragSource) { background.color = DragSourceColor; return; }

        background.color = _hovered ? HoverColor : NormalColor;
    }



    public void SetDragSourceVisual(bool isSource)
    {
        _isDragSource = isSource;
        ApplyIconAlpha();
        UpdateBackground();
    }

    private void ApplyIconAlpha()
    {
        if (icon == null) return;

        var c = icon.color;
        c.a = _isDragSource && CurrentItem != null ? 0.3f : 1f;
        icon.color = c;
    }



    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (CurrentItem == null) return;

        var drag = InventoryDragController.Instance;
        if (drag == null) return;
        drag.BeginDrag(this, eventData.position, eventData.pressEventCamera);
    }

    public void OnDrag(PointerEventData eventData)
    {
        InventoryDragController.Instance?.UpdatePosition(eventData.position, eventData.pressEventCamera);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        InventoryDragController.Instance?.EndDrag();
    }

    public void OnDrop(PointerEventData eventData)
    {
        var drag = InventoryDragController.Instance;
        if (drag == null || !drag.IsDragging) return;

        var source = drag.SourceSlot;
        if (source == null || source == this) return;

        ItemContainer.MoveOrSwap(source.Container, source.Index, _container, _index);
    }
    

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovered = true;
        UpdateBackground();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovered = false;
        UpdateBackground();
    }
}
