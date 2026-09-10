using System;
using System.Threading.Tasks;
using DevLib.ServiceLocator;
using UnityEngine;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;

namespace WorkFolder.SUH.Scripts.Temp
{
    /// <summary>
    /// PlayerDataStoreService를 ServiceLocator에서 가져와 저장/조회 동작을 검증한다.
    /// 테스트가 끝나면 변경한 값을 원래 값으로 복구한다.
    /// </summary>
    public sealed class DataStoreSmokeTest : MonoBehaviour
    {
        [SerializeField] private bool runOnStart = true;

        private bool _isRunning;

        private async void Start()
        {
            if (runOnStart)
                await RunTestAsync();
        }

        [ContextMenu("Run PlayerDataStoreService Test")]
        private async void RunFromContextMenu()
        {
            await RunTestAsync();
        }

        private async Task RunTestAsync()
        {
            if (_isRunning)
                return;

            _isRunning = true;
            IDataStoreService service = ServiceLocator.Get<IDataStoreService>();

            if (service == null)
            {
                Debug.LogError(
                    "[DataStoreSmokeTest] IDataStoreService가 등록되지 않았습니다. " +
                    "씬에 PlayerDataStoreService 컴포넌트를 추가하세요.", this);
                _isRunning = false;
                return;
            }

            string originalPlayerName = null;
            uint originalBodyColor = 0;
            bool originalValuesLoaded = false;

            try
            {
                originalPlayerName = await service.GetAsync<string>(
                    nameof(PlayerDataStore.PlayerName));
                originalBodyColor = await service.GetAsync<uint>(
                    nameof(PlayerDataStore.BodyColor));
                originalValuesLoaded = true;

                string testPlayerName = $"ServiceLocatorTest_{Guid.NewGuid():N}";
                const uint testBodyColor = 77;

                await service.SetAsync(
                    nameof(PlayerDataStore.PlayerName), testPlayerName);
                await service.SetAsync(
                    nameof(PlayerDataStore.BodyColor), testBodyColor);

                string loadedPlayerName = await service.GetAsync<string>(
                    nameof(PlayerDataStore.PlayerName));
                uint loadedBodyColor = await service.GetAsync<uint>(
                    nameof(PlayerDataStore.BodyColor));

                if (loadedPlayerName != testPlayerName || loadedBodyColor != testBodyColor)
                {
                    throw new InvalidOperationException(
                        $"저장값과 조회값이 다릅니다. " +
                        $"PlayerName={loadedPlayerName}, BodyColor={loadedBodyColor}");
                }

                Debug.Log(
                    "[DataStoreSmokeTest] PASS - ServiceLocator 등록, 값 저장, 값 불러오기 성공",
                    this);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[DataStoreSmokeTest] FAIL\n{exception}", this);
            }
            finally
            {
                if (originalValuesLoaded)
                {
                    try
                    {
                        await service.SetAsync(
                            nameof(PlayerDataStore.PlayerName), originalPlayerName);
                        await service.SetAsync(
                            nameof(PlayerDataStore.BodyColor), originalBodyColor);
                        Debug.Log("[DataStoreSmokeTest] 원래 플레이어 값으로 복구했습니다.", this);
                    }
                    catch (Exception restoreException)
                    {
                        Debug.LogError(
                            $"[DataStoreSmokeTest] 원래 값 복구 실패\n{restoreException}", this);
                    }
                }

                _isRunning = false;
            }
        }
    }
}
