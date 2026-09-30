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
using WorkFolder.SUH.Scripts.DataStoreService.Lib;

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
        private readonly ServerRequestCache<LeaderboardScoresPage> requests = new ServerRequestCache<LeaderboardScoresPage>(10);
        
        private void Start() => Configure();

        private void Configure()
        {
            leaderBoardId = leaderboardSettingSO != null && !string.IsNullOrWhiteSpace(leaderboardSettingSO.LeaderboardId)
                ? leaderboardSettingSO.LeaderboardId : "Global_Leaderboard";
            offset = (ushort)Mathf.Clamp(leaderboardSettingSO != null ? leaderboardSettingSO.LeaderboardFindOffset : 0, 0, ushort.MaxValue);
            limit = (ushort)Mathf.Clamp(leaderboardSettingSO != null ? leaderboardSettingSO.MaxVisibleRank : 10, 1, 100);
        }
        public async void GetTopRanker()
        {
            if (!requests.CanStart) return;
            try
            {
                Configure(); // Also valid when a UnityEvent fires before Start.
                var scoresResponse = await requests.GetOrRun(async () =>
                {
                    if (context == null || leaderboardTemplate == null)
                        throw new InvalidOperationException("리더보드 UI 연결이 없습니다.");
                    await UgsSignIn.EnsureSignedInAsync();
                    var sender = ServiceLocator.Get<ILeaderboardSender>();
                    if (sender == null) throw new InvalidOperationException("리더보드 저장 서비스가 준비되지 않았습니다.");
                    await sender.TryUpdatePlayer();
                    return await LeaderboardsService.Instance.GetScoresAsync(leaderBoardId,
                        new GetScoresOptions { Offset = offset, Limit = limit, IncludeMetadata = true });
                });
                if (this == null || context == null) return;
                GetLeaderboard(scoresResponse);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[리더보드] 조회 실패. 기존 목록을 유지하며 잠시 후 다시 시도할 수 있습니다: {e.Message}");
            }
        }

        private void GetLeaderboard(LeaderboardScoresPage scoresResponse)
        {
            if (scoresResponse == null) return;
            // A successful empty response must clear stale rows too.
            foreach (Transform child in context) Destroy(child.gameObject);
            if (scoresResponse != null && scoresResponse.Results.Count > 0)
            {
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
                    playerName = splitName.Length > 1 ? playerNameBuilder.ToString() : playerName;
                    double score = entry.Score;
                    uint kills = 0;

                    if (!string.IsNullOrEmpty(entry.Metadata))
                    {
                        try
                        {
                            var metaDataObj = JsonConvert.DeserializeAnonymousType(entry.Metadata, new { kills = 0U });
                            kills = metaDataObj?.kills ?? 0;
                        }
                        catch (JsonException) { /* One malformed entry must not abort the entire list. */ }
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
