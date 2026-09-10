using System;
using DevLib.ModuleSystem;
using TMPro;
using UnityEngine;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;

namespace WorkFolder.SUH.Scripts.CharacterVisual.UserInterface
{
    public class BodyColorInterface : MonoBehaviour, IAfterInitModule
    {
        [SerializeField] private TextMeshProUGUI textLabel_CurrentIndex;
        private IDataStoreService service;

        public void AfterInit()
        {
            service = DevLib.ServiceLocator.ServiceLocator.Get<IDataStoreService>();
        }

        public async void BodyColorTextChange()
        {
            AfterInit();
            
            uint value = await service?.GetAsync<uint>(nameof(PlayerDataStore.BodyType))!;
            textLabel_CurrentIndex.SetText(value.ToString());
        }
    }
}