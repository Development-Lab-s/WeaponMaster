using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevLib.ServiceLocator;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using System.Reflection;
using Unity.Services.CloudCode;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models.Data.Player;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;

namespace WorkFolder.SUH.Scripts.DataStoreService
{
    public class PlayerDataStoreService : MonoBehaviour, IAsyncInitModule, IDataStoreService
    {
        private PlayerDataStore playerDataStore;
        private readonly string storeName = "V1";
        private IDataStore<PlayerDataStore> _store;
        private Task _initializationTask;
        
        private void Awake()
        {
            ServiceLocator.TryGet<IDataStoreService>(out var registeredService);
            if (registeredService != null && !ReferenceEquals(registeredService, this))
            {
                Destroy(gameObject);
                return;
            }

            ServiceLocator.Register<IDataStoreService>(this);
            DontDestroyOnLoad(gameObject);
        }

        private async void Start()
        {
            await AsyncInit();
        }

        private void OnDestroy()
        {
            ServiceLocator.UnRegister<IDataStoreService>(this);
        }

        public Task AsyncInit()
        {
            return EnsureInitializedAsync();
        }

        public async Task<TValue> GetAsync<TValue>(string valueName)
        {
            await EnsureInitializedAsync();

            FieldInfo field = GetField<TValue>(valueName);
            object value = field.GetValue(playerDataStore);

            return value == null ? default : (TValue)value;
        }

        public async Task<TValue> SetAsync<TValue>(string valueName, TValue value)
        {
            await EnsureInitializedAsync();

            FieldInfo field = GetField<TValue>(valueName);
            object boxedValue = value;
            string playerId = AuthenticationService.Instance.PlayerId;

            playerDataStore = await _store.UpdateAsync(
                playerId,
                data =>
                {
                    data ??= new PlayerDataStore();
                    data.PlayerName = playerDataStore.PlayerName;
                    field.SetValue(data, boxedValue);

                    return data;
                }
            );

            return value;
        }

        public async Task<TValue> GetIdAsync<TValue>(string playerId, string valueName)
        {
            await EnsureInitializedAsync();

            string key = $"{storeName}_{playerId}";                
            var keys = new HashSet<string> { key };
            var options = new LoadOptions(new PublicReadAccessClassOptions(playerId));

            var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(keys, options);
            if (!playerData.TryGetValue(key, out var item))
                return default;

            PlayerDataStore store = item.Value.GetAs<PlayerDataStore>();
            FieldInfo field = GetField<TValue>(valueName);
            object value = field.GetValue(store);
            return value == null ? default : (TValue)value;
        }

        public async Task<string> SetPlayerName<TValue>(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                throw new ArgumentException("닉네임을 입력해주세요.", nameof(playerName));
            playerName = playerName.Trim();
            await EnsureInitializedAsync();
            var arguments = new Dictionary<string, object> { { "requestedName", playerName } };
            var response = await CloudCodeService.Instance.CallEndpointAsync<PlayerNameResponse>(
                "SetUniquePlayerName", arguments);
            if (response?.result == "Conflict") return "dupe";
            if (response?.result != "Success")
                throw new InvalidOperationException("닉네임 서버에서 올바르지 않은 응답을 받았습니다.");

            playerDataStore.PlayerName = response.playerName ?? playerName;
            return playerDataStore.PlayerName;
        }

        [Serializable]
        public class PlayerNameResponse
        {
            public string result;
            public string playerName;
        }


        public async Task SaveAsync()
        {
            await EnsureInitializedAsync();
            await _store.SetAsync(AuthenticationService.Instance.PlayerId, playerDataStore);
        }

        private Task EnsureInitializedAsync()
        {
            if (_initializationTask != null
                && (_initializationTask.IsFaulted || _initializationTask.IsCanceled))
                _initializationTask = null;

            return _initializationTask ??= InitializeCoreAsync();
        }

        private async Task InitializeCoreAsync()
        {
            await UgsSignIn.EnsureSignedInAsync();

            _store = Lib.DataStoreService.GetDataStore<PlayerDataStore>(storeName);
            playerDataStore = await _store.GetAsync(AuthenticationService.Instance.PlayerId)
                              ?? new PlayerDataStore();
            
            var names = await CloudSaveService.Instance.Data.Player.LoadAsync(
                new HashSet<string> { nameof(PlayerDataStore.PlayerName) });
            if (names.TryGetValue(nameof(PlayerDataStore.PlayerName), out var name))
                playerDataStore.PlayerName = name.Value.GetAs<string>();
        }

        private static FieldInfo GetField<TValue>(string valueName)
        {
            FieldInfo field = typeof(PlayerDataStore).GetField(
                valueName,
                BindingFlags.Instance | BindingFlags.Public);

            if (field == null)
                throw new ArgumentException($"{valueName} 필드가 PlayerDataStore에 없습니다.");

            if (field.FieldType != typeof(TValue))
            {
                throw new ArgumentException(
                    $"{valueName} 필드는 {field.FieldType.Name} 타입이지만 " +
                    $"{typeof(TValue).Name} 타입으로 요청했습니다.");
            }

            return field;
        }
    }
}
