using System;
using DevLib.ModuleSystem;
using UnityEditor;
using UnityEngine;
using WorkFolder.KJS._01.Scripts;
using WorkFolder.PAP.Scripts.SO.DataFolder;

namespace WorkFolder.PAP.Scripts.WeaponSystem
{
    public class HitBox : MonoBehaviour
    {
        [SerializeField] private BodyPartDataSO part;
        private IDamageable _damageable;

        private void Start()
        {
            _damageable = GetComponentInParent<NetworkOwner>().GetComponentInChildren<IDamageable>();
        }

        public BodyPartDataSO Part => part;
        public IDamageable Damageable => _damageable;
    }
}