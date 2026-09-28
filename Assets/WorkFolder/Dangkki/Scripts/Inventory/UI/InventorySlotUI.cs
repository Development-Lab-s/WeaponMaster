using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;


public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image background;
    [SerializeField] private RawImage icon;
    [SerializeField] private bool canInteract;

    private Color NormalColor = new Color(0f, 0f, 0f, 0.5f);
    private Color HoverColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    private Color DragSourceColor = new Color(0.5f, 0.5f, 0f, 0.5f);

    private ItemContainer _container;
    private int _index = -1;
    private bool _hovered;
    private bool _isDragSource;
    private string _emptyLabel = string.Empty;

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
            icon.texture = CurrentItem.icon;
        }

        UpdateBackground();
    }

    private void UpdateBackground()
    {
        if (background == null) return;
        if (canInteract)
        {
            background.color = Color.clear;
            return;
        }

        if (_isDragSource) { background.color = DragSourceColor; return; }

        background.color = _hovered ? HoverColor : NormalColor;
    }



    public void SetDragSourceVisual(bool isSource)
    {
        _isDragSource = isSource;
        UpdateBackground();
    }



    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!canInteract) return;
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
