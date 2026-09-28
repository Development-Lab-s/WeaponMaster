using System;
using UnityEngine;

[Serializable]
public class EquipSocket
{
    public string socketId = "socket";
    public string label = "장착 칸";

    [Tooltip("지정하면 이 칸에 장착할 때 이 위치에도 같은 무기를 하나 더 꽂는다 (예: Middle2).")]
    public Transform secondTarget;

    [Tooltip("두 번째 위치가 있을 때 두 무기에 곱하는 크기 배율. 게임 안 쌍수 크기와 맞춘다.")]
    [Range(0.1f, 1f)] public float dualWeaponScale = 0.75f;
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

    // 두 번째 위치가 지정된 칸(몸통 → Middle1 + Middle2)은 무기 두 개를 줄인 크기로 꽂는다.
    // 게임 안에서 offset2가 있는 부위가 무기 두 개를 쌍수 크기로 드는 것과 같은 모습이다.
    public override void ChangePart(int bodyPartIndex, int weaponIndex)
    {
        EquipSocket socket = GetSocket(bodyPartIndex);
        if (socket == null || socket.secondTarget == null)
        {
            base.ChangePart(bodyPartIndex, weaponIndex);
            return;
        }

        SpawnPart(weaponIndex, GetTargetPart(bodyPartIndex), socket.dualWeaponScale);
        SpawnPart(weaponIndex, socket.secondTarget, socket.dualWeaponScale);
    }

    public override void TryRemovePart(int bodyPartIndex)
    {
        base.TryRemovePart(bodyPartIndex);

        EquipSocket socket = GetSocket(bodyPartIndex);
        if (socket == null || socket.secondTarget == null)
            return;

        for (int i = socket.secondTarget.childCount - 1; i >= 0; i--)
            Destroy(socket.secondTarget.GetChild(i).gameObject);
    }


    private void OnValidate()
    {
        if (Application.isPlaying && Capacity != SocketCount) Resize(SocketCount);
    }

}
