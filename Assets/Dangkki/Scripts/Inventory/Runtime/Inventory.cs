using UnityEngine;


[DisallowMultipleComponent]
public class Inventory : ItemContainer
{
    [SerializeField] private int capacity = 8;
    
    [SerializeField] private ItemData[] startingItems;

    private void Awake()
    {
        Resize(capacity);
    }

    private void Start()
    {
        if (startingItems == null) return;
        foreach (var item in startingItems)
        {
            if (item == null) continue;
            if (!AddItem(item))
                Debug.Log($"다차서 못넣음{item.displayName}", this);
        }
    }


    private void OnValidate()
    {
        if (Application.isPlaying && Capacity != capacity) Resize(capacity);
    }

}
