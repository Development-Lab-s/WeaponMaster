using DevLib.ServiceLocator;
using Fusion;
using Unity.Services.Authentication;
using UnityEngine;
using WorkFolder.PAP.Scripts.Network;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;

namespace WorkFolder.KJS.Server._00.Scripts
{
    public class PlayerSpawner : SimulationBehaviour , IPlayerJoined
    {

        [SerializeField] private NetworkObject playerPrefab;
        
        [Networked]
        public NetworkString<_32> _playerId { get; set; }
        public Color NetworkColor;

        private MeshRenderer characterRenderer;
        
        private MaterialPropertyBlock _mpb;
        private IDataStoreService _service;
        
        private void InitInfo()
        {
            _playerId = AuthenticationService.Instance.PlayerId;
            _mpb = new MaterialPropertyBlock();
        }
        
        public async void PlayerJoined(PlayerRef player)
        {
            Debug.Log($"PlayerJoined 호출됨: {player}");

            if (player == Runner.LocalPlayer)
            {
                Debug.Log("내 플레이어 생성 시도");
                Runner.Spawn(playerPrefab , new Vector3(0, 20, 0) , Quaternion.identity);
                NetworkObject localPlr = RunnerProvider.Current.Spawn(playerPrefab, new Vector3(0,4,0),Quaternion.identity,inputAuthority:RunnerProvider.Current.LocalPlayer);
                characterRenderer = localPlr.GetComponentInChildren<MeshRenderer>();

                _service = ServiceLocator.Get<IDataStoreService>();
                NetworkColor = await _service?.GetAsync<Color>(nameof(PlayerDataStore.BodyColor))!;
                
                
                InitInfo();
            }
        }

    
    }
}