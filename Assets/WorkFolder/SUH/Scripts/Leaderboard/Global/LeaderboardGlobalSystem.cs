using System;
using System.Text;
using DevLib.ServiceLocator;
using Newtonsoft.Json;
using Photon.Client.StructWrapping;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine;
using WorkFolder.SUH.Scripts.Leaderboard.Global.Lib.Interface.ServiceLocator;
using WorkFolder.SUH.Scripts.Leaderboard.Lib.Interface.ServiceLocator;
using WorkFolder.SUH.Scripts.Leaderboard.Lib.ScriptableObject;
using WorkFolder.SUH.Scripts.Leaderboard.Local.Lib.Class;

namespace WorkFolder.SUH.Scripts.Leaderboard.Global
{
    public class LeaderboardGlobalSystem : MonoBehaviour, ILeaderboardGlobalMaker
    {
        [SerializeField] private GameObject leaderboardTemplate;
        [SerializeField] private Transform context;
        
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
        public async void GetTopRanker()
        {
            await ServiceLocator.Get<ILeaderboardSender>().TryUpdatePlayer();
            try
            {
                var scoresResponse =
                    await LeaderboardsService.Instance.GetScoresAsync(leaderBoardId, new GetScoresOptions{Offset = offset, Limit = limit, IncludeMetadata = true});
                GetLeaderboard(scoresResponse);
            }
            catch (Exception e)
            {
                Debug.LogError($"[리더보드] 데이터 로드 실패: {e.Message}");
            }
        }

        private void GetLeaderboard(LeaderboardScoresPage scoresResponse)
        {
            if (scoresResponse != null && scoresResponse.Results.Count > 0)
            {
                foreach (Transform child in context)
                {
                    Destroy(child.gameObject);
                }
                for (int i = 0; i < scoresResponse.Results.Count; i++)
                {
                    var entry = scoresResponse.Results[i];
                    
                    int rank = entry.Rank + 1;
                    string playerName = entry.PlayerName ?? "알 수 없는 닉네임";
                    StringBuilder playerNameBuilder = new StringBuilder();
                    string[] splitName = playerName.Split('#');
                    for (int j = 0; j < splitName.Length - 1; j++)
                    {
                        playerNameBuilder.Append(splitName[j]);
                    }
                    playerName = playerNameBuilder.ToString();
                    double score = entry.Score;
                    uint kills = 0;

                    if (!string.IsNullOrEmpty(entry.Metadata))
                    {
                        var metaDataObj = JsonConvert.DeserializeAnonymousType(entry.Metadata, new { kills = 0U });
                        kills = metaDataObj.kills;
                    }
                    MakeLeaderboard(rank, playerName, score, kills);
                }

            }
            else
            {
                Debug.Log("리더보드에 데이터가 없습니다.");
            }
            
            /*if (scoresResponse != null && scoresResponse.Results.Count > 0)
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
            }*/
        }

        private void MakeLeaderboard(int rank, string playerName, double score, uint kills)
        {

            GameObject obj = Instantiate(leaderboardTemplate, context);
            obj.GetComponent<UpdateRankObject>().UpdateRankText(rank, playerName, kills, score);
            
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