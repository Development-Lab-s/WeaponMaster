using DevLib.ModuleSystem;
using UnityEngine;
using WorkFolder.SUH.Scripts.CharacterVisual.UserInterface.ServiceLocator;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;

namespace WorkFolder.SUH.Scripts.CharacterVisual.UserInterface
{
    public class HairStyleInterface : MonoBehaviour, IAfterInitModule, IHairStyleInterfaceChange
    {
        private void Awake()
        {
            DevLib.ServiceLocator.ServiceLocator.Register<IHairStyleInterfaceChange>(this);
        }

        private void OnDestroy()
        {
            DevLib.ServiceLocator.ServiceLocator.UnRegister<IHairStyleInterfaceChange>(this);
        }

        private IDataStoreService service;
        
        
        public void AfterInit()
        {
            service = DevLib.ServiceLocator.ServiceLocator.Get<IDataStoreService>();
            

        }
        

        public async void HairStyleInterfaceChange(uint index)
        {
            AfterInit();
            
            if (service == null) return;
            await service.SetAsync<uint>(nameof(PlayerDataStore.HairStyle), index);
        }
    }
}
