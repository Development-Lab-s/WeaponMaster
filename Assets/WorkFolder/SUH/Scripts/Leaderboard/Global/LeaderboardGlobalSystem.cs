using System;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine;
using WorkFolder.SUH.Scripts.Leaderboard.Global.Lib.Interface.ServiceLocator;
using WorkFolder.SUH.Scripts.Leaderboard.Lib.ScriptableObject;

namespace WorkFolder.SUH.Scripts.Leaderboard.Global
{
    public class LeaderboardGlobalSystem : MonoBehaviour, ILeaderboardGlobalMaker
    {
        private void Awake()
        {
            DevLib.ServiceLocator.ServiceLocator.Register<ILeaderboardGlobalMaker>(this);
        }

        private void OnDestroy()
        {
            DevLib.ServiceLocator.ServiceLocator.UnRegister<ILeaderboardGlobalMaker>(this);
        }

          public Action<LocalLeaderboardEntry> OnLeaderboardUpdated;
        
        [SerializeField] private LeaderboardSettingSO leaderboardSettingSO;

        private string leaderBoardId;
        private ushort offset;
        private ushort limit;
        
        private void Start()
        {
            if (!leaderboardSettingSO)
                return;
            if (leaderboardSettingSO.RefreshIntervalSeconds == null)
                return;
            leaderBoardId = leaderboardSettingSO.LeaderboardId == null ? "Global_Leaderboard" : leaderboardSettingSO.LeaderboardId ;
            offset = (ushort)(leaderboardSettingSO.LeaderboardFindOffset == null ? 0 : leaderboardSettingSO.LeaderboardFindOffset);
            limit = (ushort)(leaderboardSettingSO.MaxVisibleRank == null ? 0 : leaderboardSettingSO.MaxVisibleRank);
            
        }
        
        public async void AddScore()
        {
            var scoreResponse = await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderBoardId, 102);
        }

        public async void GetScores()
        {
            var scoresResponse =
                await LeaderboardsService.Instance.GetScoresAsync(leaderBoardId);
        }

        public async void GetPlayerScore()
        {
            var scoreResponse =
                await LeaderboardsService.Instance.GetPlayerScoreAsync(leaderBoardId);
        }
        
     
        public async void GetTopRanker(string storeName, ushort RankerCount)
        {
            var scoresResponse =
                await LeaderboardsService.Instance.GetScoresAsync(leaderBoardId, new GetScoresOptions{Offset = offset, Limit = limit});
            MakeLeaderboard(scoresResponse);
            
           
        }

        private void MakeLeaderboard(LeaderboardScoresPage scoresResponse)
        {
            if (scoresResponse != null && scoresResponse.Results.Count > 0)
            {
                for (int i = 0; i < scoresResponse.Results.Count; i++)
                {
                    var score = scoresResponse.Results[i];
                    Debug.Log($"순위 {i + 1}: 유저 ID = {score.PlayerId}, 점수 = {score.Score}");
                }
            }
            else
            {
                Debug.Log("리더보드에 데이터가 없습니다.");
            }
        }
    }
}

/*public async void GetTopRanker(string storeName, ushort RankerCount)
     {
         try
         {
             var topScores = await LeaderboardsService.Instance.GetScoresAsync("test_Leaderboard", new GetScoresOptions
             {
                 Limit = RankerCount
             });

             if (topScores != null && topScores.Results.Count > 0)
             {
                 for (int i = 0; i < topScores.Results.Count; i++)
                 {
                     var score = topScores.Results[i];
                     Debug.Log($"순위 {i + 1}: 유저 ID = {score.PlayerId}, 점수 = {score.Score}");
                 }
             }
             else
             {
                 Debug.Log("리더보드에 데이터가 없습니다.");
             }
         }
         catch (Exception e)
         {
             Debug.LogError($"리더보드 데이터를 가져오는 중 오류가 발생했습니다: {e.Message}");
         }
     }*/