using UnityEngine;

namespace WorkFolder.SUH.Scripts.Leaderboard.Lib.ScriptableObject
{
    [CreateAssetMenu(fileName = "leaderboardSetting data", menuName = "SO/Suxghui/Leaderboard/Setting SO", order = 0)]
    public class LeaderboardSettingSO : UnityEngine.ScriptableObject
    {
        [Header("리더보드 설정")]
        public string LeaderboardId;
        
        
        [Header("리더보드 세부 설정")]
        [Tooltip("리더보드에 최대로 표시될 순위")]
        public ushort MaxVisibleRank;
        [Tooltip("리더보드에서 몇위부터 가져올 것인지.")]
        public ushort LeaderboardFindOffset;
        [Tooltip("리더보드 새로고침 버튼을 눌르고 다시 새로고침을 할 수 있는 초")]
        public float RefreshIntervalSeconds;
    }
}