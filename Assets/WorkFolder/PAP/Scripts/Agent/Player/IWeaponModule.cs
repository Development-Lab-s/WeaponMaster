using UnityEngine;
using WorkFolder.PAP.Scripts.Agent.Part;
using WorkFolder.PAP.Scripts.SO.DataFolder;

namespace WorkFolder.PAP.Scripts.Agent.Player
{
    public interface IWeaponModule
    {
        public bool IsEquipped { get; }
        public GameObject GameObject { get; }
        public void SetPartData(AbstractPartDataSO partData);
        public void AttachWeapon();
        public Vector2 WeaponMoveDirection { get; }
        void EquipWeapon(bool isEquip);
        public void SetWeaponMoveDirection(Vector2 dir);
    }
}