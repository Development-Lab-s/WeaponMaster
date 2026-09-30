using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevLib.ModuleSystem;
using DevLib.ServiceLocator;
using Newtonsoft.Json;
using Unity.Services.Authentication;
using Unity.Services.Authentication.Components;
using Unity.Services.CloudSave.Internal;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Exceptions;
using UnityEngine;
using WorkFolder.PAP.Scripts.GameSystem.KillSystem;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;
using WorkFolder.SUH.Scripts.Leaderboard.Lib.Interface.ServiceLocator;
using WorkFolder.SUH.Scripts.Leaderboard.Lib.ScriptableObject;
using WorkFolder.SUH.Scripts.Leaderboard.Local.Lib.Interface.ServiceLocator;

namespace WorkFolder.SUH.Scripts.Leaderboard.Global
{
    public class LeaderboardSetter : MonoBehaviour,
        IAfterInitModule, ILeaderboardSender
    {

        private IDataStoreService _service;
        
        [SerializeField] private LeaderboardSettingSO leaderboardSettingsSO;
        
        private void Awake()
        {
            DevLib.ServiceLocator.ServiceLocator.Register<ILeaderboardSender>(this);
        }

        private void OnDestroy()
        {
            DevLib.ServiceLocator.ServiceLocator.UnRegister<ILeaderboardSender>(this);
        }

        
        public void AfterInit()
        {
            _service = ServiceLocator.Get<IDataStoreService>();

        }

        [ContextMenu("UpdateLocalPlayer")]
        public async Task TryUpdatePlayer()
        {
            Debug.Log("Awake");
            AfterInit();
            
            if (string.IsNullOrEmpty(leaderboardSettingsSO.LeaderboardId))
            {
                Debug.LogError("[리더보드] Leaderboard ID가 설정되지 않았습니다.");
                return;
            }

            // Limit이 유효한지 체크
            if (leaderboardSettingsSO.MaxVisibleRank <= 0)
            {
                Debug.LogError($"[리더보드] Limit 값이 유효하지 않습니다: {leaderboardSettingsSO.MaxVisibleRank} (1 이상이어야 합니다)");
                return;
            }
            
            try
            {
                uint score = await _service?.GetAsync<uint>(nameof(PlayerDataStore.Score))!;
                uint killCount = await _service?.GetAsync<uint>(nameof(PlayerDataStore.KillCount))!;

                // Leaderboards metadata is limited to 1 KB. Store only small
                // score-related data here; the face PNG remains in Cloud Save.
                var metadataDict = new Dictionary<string, object>
                {
                    { "kills", killCount }
                };

                var options = new AddPlayerScoreOptions { Metadata = metadataDict };
                string updatedName = await _service?.GetAsync<string>(nameof(PlayerDataStore.PlayerName));
                updatedName = updatedName.Replace(" ", "_");
                await AuthenticationService.Instance.UpdatePlayerNameAsync(updatedName);
                var result = await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardSettingsSO.LeaderboardId, Convert.ToDouble(score), options);
                Debug.Log($"[리더보드] 점수 갱신 완료. 현재 순위: {result.Rank + 1}");
            }
            catch (LeaderboardsException le) // UGS 전용 예외 처리
            {
                Debug.LogError($"[리더보드 API 에러] Code: {le.Reason}, Message: {le.Message}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[리더보드 알 수 없는 에러] {e.ToString()}");
            }
        }
    }
}
