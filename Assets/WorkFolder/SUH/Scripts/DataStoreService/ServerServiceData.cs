using DevLib.ServiceLocator;
using Unity.Services.Authentication;
using UnityEngine;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;

namespace WorkFolder.SUH.Scripts.DataStoreService
{
    public class ServerServiceData : MonoBehaviour, IServerToPlayerSender
    {
        private void Awake()
        {
            DevLib.ServiceLocator.ServiceLocator.Register<IServerToPlayerSender>(this);
        }

        private void OnDestroy()
        {
            DevLib.ServiceLocator.ServiceLocator.UnRegister<IServerToPlayerSender>(this);
        }

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

            await service?.SetAsync(nameof(PlayerDataStore.AccessoryStyle), hairStyle)!;
        }
    }
}