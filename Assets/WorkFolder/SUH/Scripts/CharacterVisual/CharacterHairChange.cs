using DevLib.ServiceLocator;
using UnityEngine;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;

namespace WorkFolder.SUH.Scripts.CharacterVisual
{
    public class CharacterHairChange : MonoBehaviour
    {
        public async void HairChange()
        {
            IDataStoreService service = ServiceLocator.Get<IDataStoreService>();

            uint currentHairStyle = await service?.GetAsync<uint>(nameof(PlayerDataStore.HairStyle))!;
            
            
        }
    }
}