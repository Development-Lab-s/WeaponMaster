using DevLib.ModuleSystem;
using UnityEngine;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;

namespace WorkFolder.SUH.Scripts.CharacterVisual.UserInterface
{
    public class HairStyleInterface : MonoBehaviour, IAfterInitModule
    {
        private IDataStoreService service;
        
        
        public void AfterInit()
        {
            service = DevLib.ServiceLocator.ServiceLocator.Get<IDataStoreService>();
        }
        
        public async void HairStyleTextChange()
        {
            AfterInit();

            uint currentHairStyle = await service.GetAsync<uint>(nameof(PlayerDataStore.HairStyle));
            
        }
        
    }
}