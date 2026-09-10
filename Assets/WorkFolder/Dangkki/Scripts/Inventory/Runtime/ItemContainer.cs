using System;
using UnityEngine;


public abstract class ItemContainer : MonoBehaviour
{
    [SerializeField] protected ItemData[] slots = Array.Empty<ItemData>();
    [SerializeField] private Transform[] targetParts;
    [SerializeField] private GameObject[] weaponPrefabs;
    

    public event Action<ItemContainer, int> SlotChanged;
    public event Action<ItemContainer> StructureChanged;
    public int Capacity => slots?.Length ?? 0;
    public bool IsValidIndex(int index) => slots != null && index >= 0 && index < slots.Length;

    public ItemData Get(int index) => IsValidIndex(index) ? slots[index] : null;
    [SerializeField] private bool isCanAccept;


    public void Set(int index, ItemData item)
    {
        if (!IsValidIndex(index)) return;

        slots[index] = item;
        SlotChanged?.Invoke(this, index);
    }


    public virtual bool CanAccept(int index, ItemData item)
    {
        return isCanAccept;
    }

    protected void Resize(int newCapacity)
    {
        newCapacity = Mathf.Max(0, newCapacity);
        if (slots != null && slots.Length == newCapacity) return;

        var next = new ItemData[newCapacity];
        if (slots != null)
        {
            int copy = Mathf.Min(slots.Length, newCapacity);
            Array.Copy(slots, next, copy);
        }
        slots = next;
        StructureChanged?.Invoke(this);
    }

    public bool AddItem(ItemData item)
    {
        if (item == null) return false;

        int index = FindFreeSlot(item);
        if (index < 0) return false;

        Set(index, item);
        return true;
    }


    private int FindFreeSlot(ItemData item)
    {
        for (int i = 0; i < Capacity; i++)
        {
            if (slots[i] != null) continue;
            if (!CanAccept(i, item)) continue;
            return i;
        }
        return -1;
    }

    public void ChangePart(int bodyPartIndex, int weaponIndex)
    {
        GameObject targetPart = Instantiate(weaponPrefabs[weaponIndex],targetParts[bodyPartIndex]);
        targetPart.transform.localPosition = Vector3.zero;
        targetPart.transform.localRotation = Quaternion.identity;
    }

    public void TryRemovePart(int bodyPartIndex)
    {
        if (targetParts[bodyPartIndex].GetChild(0))
        {
            Destroy(targetParts[bodyPartIndex].GetChild(0).gameObject);
        }
    }




    public static bool MoveOrSwap(ItemContainer from, int fromIndex, ItemContainer to, int toIndex)
    {
        if (from == null || to == null) return false;
        if (from == to && fromIndex == toIndex) return false;
        if (!from.IsValidIndex(fromIndex) || !to.IsValidIndex(toIndex)) return false;

        var src = from.Get(fromIndex);
        if (src == null) return false;
        if (!to.CanAccept(toIndex, src)) return false;
        
        

        for (int i = 0; i < to.slots.Length; i++)
        {
            var dst = to.Get(i);
            if (to.Get(i) != null)
            {
                to.Set(i, null);
                from.Set(from.slots.Length, dst);
                to.TryRemovePart(i);
            }
        }

        to.ChangePart(toIndex, src.PrefabId);

        to.Set(toIndex, src);
        return true;
    }
}
