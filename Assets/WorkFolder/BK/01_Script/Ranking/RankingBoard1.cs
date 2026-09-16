using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RankingBoard1 : MonoBehaviour
{
    private class PlayerData
    {
        public string Id;
        public string Name;
        public int Score;
        public int Kills;
    }

    [Header("상위 5줄: 1등 자리부터 순서대로 연결")]
    [SerializeField] private RankingRow[] topRows = new RankingRow[5];

    [Header("아래 내 정보 줄")]
    [SerializeField] private RankingRow myRow;

    [Header("나를 구분하는 ID")]
    [SerializeField] private string myPlayerId;

    private readonly Dictionary<string, PlayerData> players =
        new Dictionary<string, PlayerData>();

    private void OnEnable()
    {
        RefreshRanking();
    }

    // 내가 누구인지 설정
    public void SetMyPlayerId(string playerId)
    {
        myPlayerId = playerId;
        RefreshRanking();
    }

    // 새 플레이어라면 등록, 이미 있으면 정보 수정
    // score와 kills에는 증가량이 아닌 현재 총합을 전달
    public void SetPlayerData(
        string playerId,
        string playerName,
        int score,
        int kills)
    {
        if (string.IsNullOrWhiteSpace(playerId))
        {
            Debug.LogWarning("플레이어 ID가 비어 있습니다.", this);
            return;
        }

        if (!players.TryGetValue(playerId, out PlayerData player))
        {
            player = new PlayerData
            {
                Id = playerId
            };

            players.Add(playerId, player);
        }

        player.Name = playerName;
        player.Score = score;
        player.Kills = kills;

        RefreshRanking();
    }

    // 목록에서 플레이어 제거
    public void RemovePlayer(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return;

        if (players.Remove(playerId))
        {
            RefreshRanking();
        }
    }

    // 목록 전체 비우기
    public void ClearPlayers()
    {
        players.Clear();
        RefreshRanking();
    }

    private void RefreshRanking()
    {
        // 전체 플레이어 정렬
        // 동점이면 ID순으로 순서를 고정
        List<PlayerData> sortedPlayers = players.Values
            .OrderByDescending(player => player.Score)
            .ThenBy(player => player.Id, StringComparer.Ordinal)
            .ToList();

        // 상위 5명 표시
        for (int i = 0; i < topRows.Length; i++)
        {
            RankingRow row = topRows[i];

            if (row == null)
                continue;

            if (i < 5 && i < sortedPlayers.Count)
            {
                PlayerData player = sortedPlayers[i];

                row.SetData(
                    i + 1,
                    player.Name,
                    player.Score,
                    player.Kills
                );
            }
            else
            {
                row.Clear();
            }
        }

        if (myRow == null)
            return;

        // 상위 5명이 아닌 전체 목록에서 나를 찾음
        int myIndex = sortedPlayers.FindIndex(
            player => player.Id == myPlayerId
        );

        if (myIndex < 0)
        {
            myRow.Clear();
            return;
        }

        PlayerData me = sortedPlayers[myIndex];

        myRow.SetData(
            myIndex + 1,
            me.Name,
            me.Score,
            me.Kills
        );
    }
}