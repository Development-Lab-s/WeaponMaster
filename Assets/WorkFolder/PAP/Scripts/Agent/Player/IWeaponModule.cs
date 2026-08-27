namespace WorkFolder.PAP.Scripts.Agent.Player
{
    public interface IWeaponModule
    {
        public bool IsEquipped { get; }
        void EquipWeapon(bool isEquip);
    }
}