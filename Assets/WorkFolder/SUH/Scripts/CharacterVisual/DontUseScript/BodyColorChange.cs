using DevLib.ServiceLocator;
using UnityEngine;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;

namespace WorkFolder.SUH.Scripts.CharacterVisual.CharacterVisual
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

            myColor = await service.GetAsync<Color>(nameof(PlayerDataStore.BodyColor))!;
            
            playerMaterial.SetColor("_Color", myColor);
        }
    }
}