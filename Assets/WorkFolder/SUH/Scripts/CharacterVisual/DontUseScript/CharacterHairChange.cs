using DevLib.ServiceLocator;
using UnityEngine;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;
using WorkFolder.SUH.Scripts.UserInterface.CharacterEditor.ScriptableObject;

namespace WorkFolder.SUH.Scripts.CharacterVisual.CharacterVisual
{
    public class CharacterHairChange : MonoBehaviour
    {
        [SerializeField] private HairStyleSO[] playerHairStyle;
        private HairStyleSO playerStyle;
        public async void HairChange()
        {
            IDataStoreService service = ServiceLocator.Get<IDataStoreService>();
        
            uint currentHairStyle = await service?.GetAsync<uint>(nameof(PlayerDataStore.HairStyle))!;
            
            playerStyle = playerHairStyle[currentHairStyle]!;
        }   
    }
}