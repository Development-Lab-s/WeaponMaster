using UnityEngine;

namespace WorkFolder.KJS.SO
{
    [CreateAssetMenu(menuName = "KJS/SO/EquipData" , fileName =  "EquipData")]
    public class EquipData : ScriptableObject
    {
        public string weaponName;
        public Sprite weaponImage;
        public Vector3 pivotPosition;
        public Vector3 pivotRotation;
        public Vector3 pivotScale;
    }
}