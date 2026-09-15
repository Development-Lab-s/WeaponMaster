using DevLib.ModuleSystem;
using UnityEngine;
using WorkFolder.SUH.Scripts.CharacterVisual.UserInterface.ServiceLocator;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;

namespace WorkFolder.SUH.Scripts.CharacterVisual.UserInterface
{
    public class HairStyleInterface : MonoBehaviour, IAfterInitModule, IHairStyleInterfaceChange
    {
        private IDataStoreService service;
        
        
        public void AfterInit()
        {
            service = DevLib.ServiceLocator.ServiceLocator.Get<IDataStoreService>();
            
            if (DevLib.ServiceLocator.ServiceLocator.Get<IHairStyleInterfaceChange>() == null)
                DevLib.ServiceLocator.ServiceLocator.Register(this);
        }
        

        public async void IHairStyleInterfaceChange(ushort index)
        {
            AfterInit();
            
            uint currentHairStyle = await service.GetAsync<uint>(nameof(PlayerDataStore.HairStyle));
        }
    }
}