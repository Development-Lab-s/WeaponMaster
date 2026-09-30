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
        private readonly ServerRequestCache<bool> requests = new ServerRequestCache<bool>(10);
        private string savedPlayerId, savedBoardId, savedName;
        private uint savedScore, savedKills;
        private double savedAt;
        
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
        public Task TryUpdatePlayer() => requests.GetOrRun(UpdatePlayerCore);

        private async Task<bool> UpdatePlayerCore()
        {
            AfterInit();
            if (_service == null || leaderboardSettingsSO == null || string.IsNullOrEmpty(leaderboardSettingsSO.LeaderboardId))
            {
                throw new InvalidOperationException("리더보드 서비스 또는 Leaderboard ID가 준비되지 않았습니다.");
            }

            // Limit이 유효한지 체크
            if (leaderboardSettingsSO.MaxVisibleRank <= 0)
            {
                throw new InvalidOperationException("리더보드 Limit 값은 1 이상이어야 합니다.");
            }
            
            try
            {
                await UgsSignIn.EnsureSignedInAsync();
                uint score = await _service.GetAsync<uint>(nameof(PlayerDataStore.Score));
                uint killCount = await _service.GetAsync<uint>(nameof(PlayerDataStore.KillCount));

                var metadataDict = new Dictionary<string, object>
                {
                    { "kills", killCount.ToString() }
                };

                var options = new AddPlayerScoreOptions { Metadata = metadataDict };
                string updatedName = await _service.GetAsync<string>(nameof(PlayerDataStore.PlayerName));
                if (string.IsNullOrWhiteSpace(updatedName))
                    throw new InvalidOperationException("저장된 닉네임이 없습니다.");
                updatedName = updatedName.Replace(" ", "_");
                string playerId = AuthenticationService.Instance.PlayerId;
                string boardId = leaderboardSettingsSO.LeaderboardId;
                if (savedPlayerId == playerId && savedBoardId == boardId && savedName == updatedName &&
                    savedScore == score && savedKills == killCount && Time.realtimeSinceStartupAsDouble - savedAt < 60d) return true;
                if (savedPlayerId != playerId || savedName != updatedName)
                    await AuthenticationService.Instance.UpdatePlayerNameAsync(updatedName);
                var result = await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardSettingsSO.LeaderboardId, Convert.ToDouble(score), options);
                savedPlayerId = playerId;
                savedBoardId = boardId;
                savedName = updatedName;
                savedScore = score;
                savedKills = killCount;
                savedAt = Time.realtimeSinceStartupAsDouble;
                Debug.Log($"[리더보드] 점수 갱신 완료. 현재 순위: {result.Rank + 1}");
                return true;
            }
            catch (LeaderboardsException le) // UGS 전용 예외 처리
            {
                throw new InvalidOperationException($"리더보드 갱신 실패 ({le.Reason}): {le.Message}", le);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"리더보드 갱신 실패: {e.Message}", e);
            }
        }
    }
}
