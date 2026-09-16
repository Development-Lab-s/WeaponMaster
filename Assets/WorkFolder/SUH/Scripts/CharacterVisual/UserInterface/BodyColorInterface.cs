using System;
using DevLib.ModuleSystem;
using TMPro;
using UnityEngine;
using WorkFolder.SUH.Scripts.CharacterVisual.UserInterface.ServiceLocator;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;

namespace WorkFolder.SUH.Scripts.CharacterVisual.UserInterface
{
    public class BodyColorInterface : MonoBehaviour, IAfterInitModule, IBodyColorInterfaceChange
    {
        private void Awake()
        {
            DevLib.ServiceLocator.ServiceLocator.Register<IBodyColorInterfaceChange>(this);
        }

        private void OnDestroy()
        {
            DevLib.ServiceLocator.ServiceLocator.UnRegister<IBodyColorInterfaceChange>(this);
        }

        [SerializeField] private TextMeshProUGUI textLabel_CurrentIndex;
        private IDataStoreService service;
        [SerializeField] private uint[] bodyColors = Array.Empty<uint>();

        public async void BodyColorInterfaceChange(uint index)
        {
            AfterInit();
            if (service == null) return;
            await service.SetAsync(nameof(PlayerDataStore.BodyColor), index);
            if (textLabel_CurrentIndex != null) textLabel_CurrentIndex.SetText(index.ToString());
        }

        public void AfterInit()
        {
            service = DevLib.ServiceLocator.ServiceLocator.Get<IDataStoreService>();
            

        }

        public async void BodyColorTextChange()
        {
            AfterInit();
            
            if (service == null || textLabel_CurrentIndex == null) return;
            uint value = await service.GetAsync<uint>(nameof(PlayerDataStore.BodyColor));
            textLabel_CurrentIndex.SetText(value.ToString());
        }
    }
}
