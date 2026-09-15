using System.Threading.Tasks;
using DevLib.ModuleSystem;
using UnityEditor.UIElements;
using UnityEngine;
using WorkFolder.SUH.Scripts.CharacterVisual.UserInterface.ServiceLocator;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;

namespace WorkFolder.SUH.Scripts.CharacterVisual.UserInterface
{
    public class PlayerNameInterface : MonoBehaviour, IPlayerNameInterfaceChange, IAfterInitModule
    {
        private IDataStoreService service;
        
        public void AfterInit()
        {
            service = DevLib.ServiceLocator.ServiceLocator.Get<IDataStoreService>();
            if (DevLib.ServiceLocator.ServiceLocator.Get<IPlayerNameInterfaceChange>() == null)
                DevLib.ServiceLocator.ServiceLocator.Register(this);
        }
        
        public async Task<bool> PlayerNameChange(string playerName)
        {
            var result = await service.SetPlayerName<string>(playerName);

            if (result == "dupe")
                return false; // 닉네임 중복 처리
            else
                return true; // 닉네임 생성 처리

        }
    }
}