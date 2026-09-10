using UnityEngine;


[CreateAssetMenu(fileName = "Item_", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    public string displayName = "New Item";

    public Sprite icon;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(displayName)) displayName = name;
    }
}
