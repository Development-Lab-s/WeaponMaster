using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface;

namespace WorkFolder.SUH.Scripts.DataStoreService.Lib
{
    public class UGSInitializer : MonoBehaviour, IAsyncInitModule
    {
        public async Task AsyncInit()
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"게스트 로그인 {AuthenticationService.Instance.PlayerId}");
            }
        }
    }
}