using UnityEngine;


[CreateAssetMenu(fileName = "Item_", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    public string displayName = "New Item";

    public Texture icon;

    public int PrefabId;

    public GameObject ItemPrefab;

    public Vector3 rightRot;
    public Vector3 leftRot;
    public Vector3 originRot;
}
