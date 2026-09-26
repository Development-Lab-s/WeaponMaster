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
            await UgsSignIn.EnsureSignedInAsync();
            Debug.Log($"게스트 로그인 {AuthenticationService.Instance.PlayerId}");
        }
    }
}