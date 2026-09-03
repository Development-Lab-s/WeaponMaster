using UnityEngine;

namespace WorkFolder.PAP.Scripts.Agent.Player
{
    public interface IWeaponModule
    {
        public bool IsEquipped { get; }
        public Vector2 WeaponMoveDirection { get; }
        void EquipWeapon(bool isEquip);
        public void SetWeaponMoveDirection(Vector2 dir);
    }
}