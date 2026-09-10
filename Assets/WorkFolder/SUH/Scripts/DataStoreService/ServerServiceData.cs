using DevLib.ServiceLocator;
using Unity.Services.Authentication;
using UnityEngine;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;

namespace WorkFolder.SUH.Scripts.DataStoreService
{
    public class ServerServiceData : MonoBehaviour, IServerToPlayerSender
    {
        public async void PlayerNameTextReceived(string playerName)
        {
            IDataStoreService service = ServiceLocator.Get<IDataStoreService>();

            await service?.SetAsync(nameof(PlayerDataStore.PlayerName), playerName)!;
        }

        public async void PlayerMaterialTextReceived(Color playerMaterial)
        {
            IDataStoreService service = ServiceLocator.Get<IDataStoreService>();
            
            await service?.SetAsync(nameof(PlayerDataStore.BodyColor), playerMaterial)!;
        }

        public async void PlayerHairStyleReceived(uint hairStyle)
        {
            IDataStoreService service = ServiceLocator.Get<IDataStoreService>();

            await service?.SetAsync(nameof(PlayerDataStore.HairStyle), hairStyle)!;
        }
    }
}