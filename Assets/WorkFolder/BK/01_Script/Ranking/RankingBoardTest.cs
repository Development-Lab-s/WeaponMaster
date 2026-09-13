using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RankingBoardTest : MonoBehaviour
{
    [Header("씬의 RankingBoard 연결")]
    [SerializeField] private RankingBoard1 rankingBoard;

    [Header("실행 시 테스트용 6명 자동 등록")]
    [SerializeField] private bool createSampleOnStart = true;

    [Header("수정할 플레이어 데이터")]
    [SerializeField] private string playerId = "p6";
    [SerializeField] private string playerName = "나";
    [SerializeField] private int score = 100;
    [SerializeField] private int kills = 1;

    [Header("내 정보 줄에 표시할 플레이어 ID")]
    [SerializeField] private string myPlayerId = "p6";

    // 테스트에서 등록한 이름과 킬을 보관
    private readonly Dictionary<string, (string Name, int Kills)>
        testPlayers = new Dictionary<string, (string Name, int Kills)>();

    private void Start()
    {
        if (createSampleOnStart)
            CreateSample();
    }

    private void Update()
    {
        if (Keyboard.current == null ||
            !Keyboard.current.tKey.wasPressedThisFrame)
            return;

        if (!CanRun())
            return;

        foreach (var entry in testPlayers)
        {
            // 이름과 킬은 유지하고 점수만 랜덤 변경
            rankingBoard.SetPlayerData(
                entry.Key,
                entry.Value.Name,
                Random.Range(0, 1001),
                entry.Value.Kills
            );
        }
    }

    private void SetTestPlayer(
        string id,
        string name,
        int totalScore,
        int totalKills)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        testPlayers[id] = (name, totalKills);

        rankingBoard.SetPlayerData(
            id,
            name,
            totalScore,
            totalKills
        );
    }

    [ContextMenu("1. 테스트 데이터 초기화")]
    private void CreateSample()
    {
        if (!CanRun())
            return;

        testPlayers.Clear();
        rankingBoard.ClearPlayers();
        rankingBoard.SetMyPlayerId(myPlayerId);

        SetTestPlayer("p1", "아", 600, 6);
        SetTestPlayer("p2", "집", 500, 5);
        SetTestPlayer("p3", "가고", 400, 4);
        SetTestPlayer("p4", "싶다", 300, 3);
        SetTestPlayer("p5", "진짜", 200, 2);
        SetTestPlayer("p6", "나", 100, 1);
    }

    [ContextMenu("2. 입력한 플레이어 정보 적용")]
    private void ApplyPlayerData()
    {
        if (!CanRun())
            return;

        SetTestPlayer(playerId, playerName, score, kills);
    }

    [ContextMenu("3. 내 플레이어 ID 적용")]
    private void ApplyMyPlayerId()
    {
        if (!CanRun())
            return;

        rankingBoard.SetMyPlayerId(myPlayerId);
    }

    [ContextMenu("4. 입력한 ID의 플레이어 삭제")]
    private void RemovePlayer()
    {
        if (!CanRun() || string.IsNullOrWhiteSpace(playerId))
            return;

        testPlayers.Remove(playerId);
        rankingBoard.RemovePlayer(playerId);
    }

    private bool CanRun()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Play 모드에서 실행하세요.", this);
            return false;
        }

        if (rankingBoard == null)
        {
            Debug.LogWarning("Ranking Board를 연결하세요.", this);
            return false;
        }

        return true;
    }
}