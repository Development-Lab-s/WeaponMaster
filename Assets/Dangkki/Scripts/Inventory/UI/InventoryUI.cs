using UnityEngine;
using UnityEngine.UI;


[DisallowMultipleComponent]
public class InventoryUI : MonoBehaviour // 깡통 
{
    public enum LayoutMode
    {
        //칸 자동 정렬
        Grid = 0,
        // 걍 냅둠
        Free = 1,
    }

    [Header("데이터")]
    [SerializeField] private ItemContainer container;

    [Header("배치")]
    [SerializeField] private LayoutMode layoutMode = LayoutMode.Grid;

    [SerializeField] private RectTransform gridParent;
    
    [SerializeField] private Vector2 cellSize = new Vector2(260, 64);
    [SerializeField] private Vector2 spacing = new Vector2(8, 8);
    [SerializeField] private InventorySlotUI slotPrefab;
    [SerializeField] private InventorySlotUI[] fixedSlots;

    private readonly System.Collections.Generic.List<InventorySlotUI> _slots = new();
    private GridLayoutGroup _grid;
    private Canvas _canvas;

    private void Awake()
    {
        if (container == null) container = GetComponentInParent<ItemContainer>();

        _canvas = GetComponentInParent<Canvas>();
        if (_canvas != null)
        {
            _canvas = _canvas.rootCanvas;
            InventoryDragController.GetOrCreate(_canvas);
        }

        EnsureGrid();
    }

    private void OnEnable()
    {
        if (container == null) return;
        container.SlotChanged += OnSlotChanged;
        container.StructureChanged += OnStructureChanged;
    }

    private void OnDisable()
    {
        if (container == null) return;
        container.SlotChanged -= OnSlotChanged;
        container.StructureChanged -= OnStructureChanged;
    }

    private void Start()
    {
        Rebuild();
    }

    private void EnsureGrid()
    {
        if (layoutMode == LayoutMode.Free) return;

        if (gridParent == null)
        {
            var go = new GameObject("Grid", typeof(RectTransform));
            gridParent = (RectTransform)go.transform;
            gridParent.SetParent(transform, false);
            gridParent.anchorMin = new Vector2(0, 0);
            gridParent.anchorMax = new Vector2(1, 1);
            gridParent.offsetMin = new Vector2(12, 12);
            gridParent.offsetMax = new Vector2(-12, -12);
        }

        _grid = gridParent.GetComponent<GridLayoutGroup>();
        if (_grid == null) _grid = gridParent.gameObject.AddComponent<GridLayoutGroup>();

        _grid.cellSize = cellSize;
        _grid.spacing = spacing;
        _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        _grid.constraintCount = 1;
        _grid.childAlignment = TextAnchor.UpperCenter;
    }

    [ContextMenu("Rebuild")]
    public void Rebuild()
    {
        if (layoutMode == LayoutMode.Free)
        {
            BindExistingSlots();
            return;
        }
        
        EnsureGrid();

        for (int i = _slots.Count - 1; i >= 0; i--)
            if (_slots[i] != null) Destroy(_slots[i].gameObject);
        _slots.Clear();

        var sockets = container as EquipSocketContainer;

        for (int i = 0; i < container.Capacity; i++)
        {
            var slot = Instantiate(slotPrefab, gridParent);

            slot.name = string.Format("Slot_{0:00}", i);
            slot.Bind(container, i);
            
            
            var socket = sockets != null ? sockets.GetSocket(i) : null;
            if (socket != null)
            {
                slot.name = string.Format("Socket_{0:00}_{1}", i, socket.socketId);
                slot.EmptyLabel = socket.label;
            }

            _slots.Add(slot);
        }
    }

    private void BindExistingSlots()
    {
        _slots.Clear();

        if (fixedSlots != null && fixedSlots.Length > 0)
            _slots.AddRange(fixedSlots);
        else
            _slots.AddRange(GetComponentsInChildren<InventorySlotUI>(true));

        var sockets = container as EquipSocketContainer;

        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            
            slot.Bind(container, i);

            var socket = sockets != null ? sockets.GetSocket(i) : null;
            if (socket != null) slot.EmptyLabel = socket.label;
        }
        
    }

    private void OnSlotChanged(ItemContainer c, int index)
    {
        if (index >= 0 && index < _slots.Count && _slots[index] != null)
            _slots[index].Refresh();
    }

    private void OnStructureChanged(ItemContainer c) => Rebuild();
}
