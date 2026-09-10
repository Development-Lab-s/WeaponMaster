using System;
using DevLib.ServiceLocator;
using UnityEngine;
using WorkFolder.PAP.Scripts.GameSystem.KillSystem;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;

namespace WorkFolder.SUH.Scripts.CharacterVisual
{
    public class BodyColorChange : MonoBehaviour
    {
        [SerializeField] private Material playerMaterial;

        private Color myColor;
        
        public async void ColorChange()
        {
            IDataStoreService service = ServiceLocator.Get<IDataStoreService>();
            
            if (service == null)
                return;

            myColor = await service.GetAsync<Color>(nameof(PlayerDataStore.BodyColor));
            
            if(ColorUtility.TryParseHtmlString("FF8535", out myColor))
                playerMaterial.SetColor("_Color", myColor);
        }
    }
}