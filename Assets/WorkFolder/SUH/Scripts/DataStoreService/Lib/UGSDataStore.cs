using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using UnityEngine;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models.Data.Player;
using Unity.Services.Core;
using SaveOptions = Unity.Services.CloudSave.Models.Data.Player.SaveOptions;

namespace WorkFolder.SUH.Scripts.DataStoreService.Lib
{
    public class UGSDataStore<T> : IDataStore<T> where T : class, new()
    {
        private readonly string _storeName;

        public UGSDataStore(string storeName)
        {
            _storeName = storeName;
        }

        private string GetKey(string key) => $"{_storeName}_{key}";


        private async Task EnsureInitializedAsync()
        {
            if (UnityServices.State  == ServicesInitializationState.Uninitialized)
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        
        public async Task<T> GetAsync(string key)
        {
            await EnsureInitializedAsync();
            
            string fullKey = GetKey(key);

            try
            {
                var result = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { fullKey },
                    new LoadOptions(new PublicReadAccessClassOptions()));
                if (result.TryGetValue(fullKey, out var item))
                {
                    return item.Value.GetAs<T>();
                }

                return new T();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                return new T();
            }
        }

        public async Task SetAsync(string key, T value)
        {
            await EnsureInitializedAsync();
            
            string fullKey = GetKey(key);
            
            try
            {
                var data = new Dictionary<string, object> { { fullKey, value } };
                await CloudSaveService.Instance.Data.Player.SaveAsync(data, new SaveOptions(new PublicWriteAccessClassOptions()));
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        }

        public async Task<T> UpdateAsync(string key, Func<T, T> transformFunc)
        {
            T currentValue = await GetAsync(key);
            T updated = transformFunc(currentValue);
            await SetAsync(key, updated);
            return updated;
        }

        public async Task DeleteAsync(string key)
        {
            await EnsureInitializedAsync();
            
            string fullKey = GetKey(key);

            try
            {
                await CloudSaveService.Instance.Data.Player.DeleteAsync(fullKey);
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
            }
        }
    }
}