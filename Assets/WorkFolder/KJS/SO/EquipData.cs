using System;
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
        
        public float mass;
        public float baseForce;
        public float rampRate;
        public float maxForce;
        public float drag;
        
    }
}