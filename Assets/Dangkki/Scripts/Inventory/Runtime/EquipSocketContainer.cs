using System;
using UnityEngine;

[Serializable]
public class EquipSocket
{
    public string socketId = "socket";
    public string label = "장착 칸";
}



public class EquipSocketContainer : ItemContainer
{
    [SerializeField] private GameObject targetObject;
    [Header("장착 칸")] 
    [SerializeField]
    private EquipSocket[] sockets = {};

    public int SocketCount => sockets?.Length ?? 0;

    private void Awake()
    {
        Resize(SocketCount);
    }

    public (int, ItemData) GetCurrentPartAndWeapon()
    {
        targetObject.SetActive(false);
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                return (i, slots[i]);
            }
        }
        return (0, null);
    }


    public EquipSocket GetSocket(int index) =>
        sockets != null && index >= 0 && index < sockets.Length ? sockets[index] : null;


    private void OnValidate()
    {
        if (Application.isPlaying && Capacity != SocketCount) Resize(SocketCount);
    }

}
