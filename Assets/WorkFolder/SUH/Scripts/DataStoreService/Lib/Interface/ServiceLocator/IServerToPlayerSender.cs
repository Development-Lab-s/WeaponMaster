using UnityEngine;

namespace WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator
{
    public interface IServerToPlayerSender
    {
        void PlayerNameTextReceived(string playerName);
        void PlayerMaterialTextReceived(Color playerMaterial);
        
        void PlayerHairStyleReceived(uint hairStyle);
    }
}