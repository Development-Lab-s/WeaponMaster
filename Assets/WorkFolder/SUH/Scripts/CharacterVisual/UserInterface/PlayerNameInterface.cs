using System;
using System.Threading.Tasks;
using DevLib.ModuleSystem;
using UnityEngine;
using WorkFolder.SUH.Scripts.CharacterVisual.UserInterface.ServiceLocator;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;

namespace WorkFolder.SUH.Scripts.CharacterVisual.UserInterface
{
    public class PlayerNameInterface : MonoBehaviour, IPlayerNameInterfaceChange, IAfterInitModule
    {
        private void Awake()
        {
            DevLib.ServiceLocator.ServiceLocator.Register<IPlayerNameInterfaceChange>(this);
        }

        private void OnDestroy()
        {
            DevLib.ServiceLocator.ServiceLocator.UnRegister<IPlayerNameInterfaceChange>(this);
        }

        private IDataStoreService service;

        private void Start()
        {
            AfterInit();
        }

        public void AfterInit()
        {
            service = DevLib.ServiceLocator.ServiceLocator.Get<IDataStoreService>();

        }
        
        public async Task<bool> PlayerNameChange(string playerName)
        {
            AfterInit();
            if (service == null)
                throw new InvalidOperationException("IDataStoreService가 등록되지 않았습니다.");
            var result = await service.SetPlayerName<string>(playerName);

            if (result == "dupe")
                return false; // 닉네임 중복 처리
            else
                return true; // 닉네임 생성 처리

        }
    }
}