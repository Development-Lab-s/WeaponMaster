using DevLib.ServiceLocator;
using Fusion;
using Unity.Services.Authentication;
using UnityEngine;
using WorkFolder.PAP.Scripts.Agent.Player;
using WorkFolder.PAP.Scripts.Network;
using WorkFolder.PAP.Scripts.SO.DataFolder;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;
using WorkFolder.SUH.Scripts.Proton.PlayerClass;

namespace WorkFolder.SUH.Scripts.Proton.Runner
{
    public class Spawner : SimulationBehaviour
    {
        [SerializeField] private EquipSocketContainer container;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private PartOffsetDataSO[] partDataList;
        private bool spawning;
        
        /*public NetworkString<_32> _playerId { get; set; }
        public Color NetworkColor;
        public uint HairStyleIndex;
        
        private MeshRenderer characterRenderer;
        
        private MaterialPropertyBlock _mpb;
        private IDataStoreService _service;
        private PlayerInfomationData plrData;*/
        /*
        private void InitInfo()
        {
            plrData = new PlayerInfomationData();

            
            plrData.PlayerId = _playerId;
            plrData.BodyColor = NetworkColor;
            plrData.HairStyle = HairStyleIndex;
        }
        */
        
        Transform FindChildHelper(Transform current, string targetName)
        {
            if (current.name == targetName) return current;

            foreach (Transform child in current)
            {
                Transform found = FindChildHelper(child, targetName);
                if (found != null) return found;
            }

            return null;
        }
        
        public async void SpawnCharacter()
        {
            if (spawning) return;
            spawning = true;
            try
            {
            await UgsSignIn.EnsureSignedInAsync();
            try
            {
                await WorkFolder.SUH.Scripts.FacePersistence.FaceStore.Load(AuthenticationService.Instance.PlayerId);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Face preload failed; spawned player will retry: {ex.Message}", this);
            }
            if (this == null || RunnerProvider.Current == null) return;
            (int, ItemData) pack = container.GetCurrentPartAndWeapon();
            
            NetworkObject player = RunnerProvider.Current.Spawn(playerPrefab, new Vector3(0,4,0),Quaternion.identity,inputAuthority:RunnerProvider.Current.LocalPlayer);
            Player playerHandler = player.GetComponent<Player>();
            PartOffsetDataSO partData = partDataList[pack.Item1];
            
            playerHandler.SetInitialize(partData,FindChildHelper(player.transform,partData.targetPartName).transform.GetComponent<NetworkObject>(),pack.Item2.ItemPrefab);
            }
            finally { spawning = false; }
            
            /*_playerId = AuthenticationService.Instance.PlayerId;
            
            _mpb = new MaterialPropertyBlock();
            characterRenderer = player.GetComponentInChildren<MeshRenderer>();

            _service = ServiceLocator.Get<IDataStoreService>();
            NetworkColor = await _service?.GetAsync<Color>(nameof(PlayerDataStore.BodyColor))!;
            HairStyleIndex = await _service?.GetAsync<uint>(nameof(PlayerDataStore.HairStyle))!;
            
        
            InitInfo();
            PlayerBodyColorChange();*/
        }
        
        /*private void PlayerBodyColorChange()
        {
            characterRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor("_BaseColor", NetworkColor);
            characterRenderer.SetPropertyBlock(_mpb);
        }*/
    }
}
