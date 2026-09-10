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
    [Header("장착 칸")]
    [SerializeField]
    private EquipSocket[] sockets = {};

    public int SocketCount => sockets?.Length ?? 0;

    private void Awake()
    {
        Resize(SocketCount);
    }


    public EquipSocket GetSocket(int index) =>
        sockets != null && index >= 0 && index < sockets.Length ? sockets[index] : null;


    private void OnValidate()
    {
        if (Application.isPlaying && Capacity != SocketCount) Resize(SocketCount);
    }

}
