using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevLib.ServiceLocator;
using Fusion;
using TMPro;
using UnityEngine;
using WorkFolder.PAP.Scripts.Agent.Player;
using WorkFolder.PAP.Scripts.MapSystem;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;
using WorkFolder.SUH.Scripts.RoundSystem.Lib;
using WorkFolder.SUH.Scripts.RoundSystem.Lib.Interface;

namespace WorkFolder.SUH.Scripts.RoundSystem
{
    public struct RoundScore : INetworkStruct
    {
        public NetworkString<_32> Name;
        public int Score;
        public int Kills;
    }

    [Serializable]
    public struct ChessBalance : INetworkStruct
    {
        [Min(1f), Tooltip("붕괴와 복원을 반복하는 전체 주기(초)")]
        public float CycleSeconds;
        [Min(0f), Tooltip("각 주기 시작 후 첫 타일 예고까지 대기(초)")]
        public float FirstWarningSeconds;
        [Min(0.1f), Tooltip("다음 타일 묶음의 예고를 시작하는 간격(초)")]
        public float BatchIntervalSeconds;
        [Min(1), Tooltip("한 번에 떨어뜨릴 타일 수")]
        public int TilesPerBatch;
        [Range(0.05f, 0.9f), Tooltip("동시에 사라질 수 있는 최대 타일 비율")]
        public float MaxMissingRatio;
        [Min(0.1f), Tooltip("흔들리는 예고 시간. 이 동안은 밟을 수 있음")]
        public float WarningSeconds;
        [Min(0.1f), Tooltip("실제로 아래로 내려가는 트윈 시간")]
        public float FallSeconds;
        [Min(1f), Tooltip("주기 시작 기준 랜덤 복원이 시작될 수 있는 가장 빠른 시점(초)")]
        public float RestoreSeconds;
        [Min(0.1f), Tooltip("각 타일의 복원 시점을 독립적으로 무작위 분산하는 시간 범위")]
        public float RestoreSpreadSeconds;
        [Min(0.1f), Tooltip("각 타일이 올라와 제자리에 채워지는 시간")]
        public float RestoreRiseSeconds;
        [Min(0f), Tooltip("스폰 지점 주변 보호 반경(월드 단위). 이 영역과 겹치는 타일은 낙하하지 않음")]
        public float SpawnProtectionRadius;

        public ChessBalance Validated()
        {
            var value = this;
            value.SpawnProtectionRadius = Mathf.Max(0f, value.SpawnProtectionRadius);
            value.FirstWarningSeconds = Mathf.Max(0f, value.FirstWarningSeconds);
            value.BatchIntervalSeconds = Mathf.Max(0.1f, value.BatchIntervalSeconds);
            value.TilesPerBatch = Mathf.Clamp(value.TilesPerBatch, 1, 81);
            value.MaxMissingRatio = Mathf.Clamp(value.MaxMissingRatio, 0.05f, 0.9f);
            value.WarningSeconds = Mathf.Max(0.1f, value.WarningSeconds);
            value.FallSeconds = Mathf.Max(0.1f, value.FallSeconds);
            value.RestoreSeconds = Mathf.Max(value.RestoreSeconds,
                value.FirstWarningSeconds + value.WarningSeconds + value.FallSeconds + 0.1f);
            value.RestoreSpreadSeconds = Mathf.Max(0.1f, value.RestoreSpreadSeconds);
            value.RestoreRiseSeconds = Mathf.Max(0.1f, value.RestoreRiseSeconds);
            value.CycleSeconds = Mathf.Max(value.CycleSeconds,
                value.RestoreSeconds + value.RestoreSpreadSeconds + value.RestoreRiseSeconds + 1f);
            return value;
        }
    }

    [RequireComponent(typeof(NetworkObject), typeof(MapService), typeof(SpawnService))]
    public sealed class RoundSystem : NetworkBehaviour, IRoundSystem
    {
        [SerializeField, Min(1f)] private float roundDurationSeconds = 284f;
        [SerializeField, Min(10f)] private float resultDurationSeconds = 10f;
        [SerializeField, Min(1f)] private float mapReadyTimeoutSeconds = 15f;
        [SerializeField] private RoundResultUI ui;
        [SerializeField] private TextMeshProUGUI roundNextTextLabel;
        [Networked] public RoundState State { get; private set; }
        [Networked] public int RoundNumber { get; private set; }
        [Networked] private TickTimer PhaseTimer { get; set; }
        [Networked] private int MapIndex { get; set; }
        [Networked] private int MapRevision { get; set; }
        [Networked] private int ChessSeed { get; set; }
        [Networked] private int FallenTileCount { get; set; }
        [Networked] private double ChessRoundStart { get; set; }
        [Networked] private int ChessCycleNumber { get; set; }
        [Header("Chess Balance (applied next round)")]
        [SerializeField] private ChessBalance chessBalance = new ChessBalance
        {
            CycleSeconds = 46f, FirstWarningSeconds = 8f, BatchIntervalSeconds = 1.25f,
            TilesPerBatch = 4, MaxMissingRatio = 0.6f, WarningSeconds = 2f,
            FallSeconds = 1.2f, RestoreSeconds = 28f, RestoreSpreadSeconds = 16f, RestoreRiseSeconds = 0.8f, SpawnProtectionRadius = 3f
        };
        [Networked] private ChessBalance ActiveChessBalance { get; set; }
        [Networked, Capacity(64)] private NetworkDictionary<PlayerRef, RoundScore> Scores => default;
        [Networked, Capacity(64)] private NetworkDictionary<PlayerRef, int> MapReady => default;

        [Networked] public int mapRoundIndex { get; private set; }

        public event Action<RoundState> StateChanged;
        public event Action<IReadOnlyList<RoundResultEntry>> ResultsReady;
        private MapService maps;
        private SpawnService spawns;
        private int appliedRevision = -1;
        private int observedRound = -1;
        private RoundState observedState = (RoundState)(-1);
        private bool scoreReadPending;
        private bool hasBaseline;
        private uint baselineScore, baselineKills;
        private string localName;
        private float nextScoreRead;
        private int displayedNextMapIndex = int.MinValue;
        private MapCatalogSO mapCatalog;
        private bool HasRoundAuthority => Object != null && Object.IsValid && HasStateAuthority;

        private void Awake()
        {
            maps = GetComponent<MapService>();
            spawns = GetComponent<SpawnService>();
            mapCatalog = MapCatalogSO.LoadDefault();
            ServiceLocator.Register<IRoundSystem>(this);
            // Scene object survives Shared master departure and transfers authority automatically.
            GetComponent<NetworkObject>().Flags |= NetworkObjectFlags.MasterClientObject;
            GetComponent<NetworkObject>().Flags &= ~NetworkObjectFlags.DestroyWhenStateAuthorityLeaves;
        }

        private void OnDestroy() => ServiceLocator.UnRegister<IRoundSystem>(this);

        public override void Spawned()
        {
            spawns.Bind(Runner);
            if (HasRoundAuthority && RoundNumber == 0)
            {
                MapIndex = 0;
                if (Runner.SessionInfo.Properties != null &&
                    Runner.SessionInfo.Properties.TryGetValue(MapCatalogSO.SessionPropertyKey, out SessionProperty property) &&
                    property.PropertyValue is int index) MapIndex = index;
                State = RoundState.Waiting;
            }
        }

        public override void FixedUpdateNetwork()
        {
            ApplyChessboardState();
            if (!HasRoundAuthority) return;
            switch (State)
            {
                case RoundState.Waiting:
                    if (maps.HasMap && Player.All != null && Player.All.Any(p => p != null && p.Object != null && p.Object.IsValid && p.Runner == Runner))
                    {
                        StartRound();
                        mapRoundIndex = maps.ChooseNextMap(MapIndex);
                        if (mapRoundIndex < 0)
                        {
                            Debug.LogError(
                                "[RoundSystem] No rotation maps registered in MapCatalog. Retrying in 10 seconds.",
                                this);
                            PhaseTimer = TickTimer.CreateFromSeconds(Runner, 10f);
                            break;
                        }
                    }

                    break;
                case RoundState.Playing:
                    if (PhaseTimer.Expired(Runner)) EndRound();
                    else UpdateChessboardCycle();
                    break;
                case RoundState.Result:
                    if (!PhaseTimer.Expired(Runner)) break;
                    MapIndex = mapRoundIndex;
                    MapRevision++;
                    FallenTileCount = 0;
                    ChessCycleNumber = 0;
                    MapReady.Clear();
                    State = RoundState.LoadingNextMap;
                    PhaseTimer = TickTimer.CreateFromSeconds(Runner, mapReadyTimeoutSeconds);
                    Runner.SessionInfo.UpdateCustomProperties(new Dictionary<string, SessionProperty>
                    {
                        [MapCatalogSO.SessionPropertyKey] = mapRoundIndex
                    });
                    break;
                case RoundState.LoadingNextMap:
                    // Slow/late clients remain behind the result screen until their own map is ready.
                    if (appliedRevision == MapRevision &&
                        (Runner.ActivePlayers.All(p => MapReady.TryGet(p, out int revision) && revision == MapRevision) || PhaseTimer.Expired(Runner)))
                        StartRound();
                    break;
            }
        }

        public void StartRound()
        {
            if (!HasRoundAuthority || (State != RoundState.Waiting && State != RoundState.LoadingNextMap)) return;
            Scores.Clear();
            ChessSeed = UnityEngine.Random.Range(1, int.MaxValue);
            FallenTileCount = 0;
            ActiveChessBalance = chessBalance.Validated();
            ChessRoundStart = Runner.SimulationTime;
            ChessCycleNumber = 0;
            RoundNumber++;
            State = RoundState.Playing;
            PhaseTimer = TickTimer.CreateFromSeconds(Runner, roundDurationSeconds);
        }

        public void EndRound()
        {
            if (!HasRoundAuthority || State != RoundState.Playing) return;
            State = RoundState.Result;
            PhaseTimer = TickTimer.CreateFromSeconds(Runner, Mathf.Max(10f, resultDurationSeconds));
        }

        private void UpdateChessboardCycle()
        {
            if (appliedRevision != MapRevision || maps.ChessTileCount == 0) return;
            ChessBalance balance = ActiveChessBalance;
            double elapsed = Math.Max(0d, Runner.SimulationTime - ChessRoundStart);
            int cycle = (int)(elapsed / balance.CycleSeconds);
            if (ChessCycleNumber != cycle)
            {
                ChessCycleNumber = cycle;
                ChessSeed = UnityEngine.Random.Range(1, int.MaxValue);
            }
            double phase = elapsed - cycle * balance.CycleSeconds;
            int limit = Mathf.Min(maps.GetFallableChessTileCount(balance.SpawnProtectionRadius),
                Mathf.Max(1, Mathf.FloorToInt(maps.ChessTileCount * balance.MaxMissingRatio)));
            // Stop scheduling early enough for the last warning and fall to finish before restoration.
            double lastWarning = balance.RestoreSeconds - balance.WarningSeconds - balance.FallSeconds;
            double schedulePhase = Math.Min(phase, lastWarning);
            FallenTileCount = phase < balance.FirstWarningSeconds ? 0 :
                Math.Min(limit, ((int)((schedulePhase - balance.FirstWarningSeconds) / balance.BatchIntervalSeconds) + 1) * balance.TilesPerBatch);
            ApplyChessboardState();
        }

        private void ApplyChessboardState(bool rendering = false)
        {
            if (appliedRevision != MapRevision || RoundNumber == 0) return;
            ChessBalance balance = ActiveChessBalance;
            double clock = rendering ? Runner.LocalRenderTime : Runner.SimulationTime;
            float fallAge = (float)(clock - ChessRoundStart -
                ChessCycleNumber * balance.CycleSeconds - balance.FirstWarningSeconds);
            maps.ApplyChessTiles(ChessSeed, FallenTileCount, fallAge, balance);
        }

        public override void Render()
        {
            spawns.ObservePlayers();
            if (appliedRevision != MapRevision && maps.HasMap)
            {
                if (MapRevision == 0) maps.AdoptInitialMap(MapIndex);
                else
                {
                    spawns.ClearPlayers();
                    if (!maps.ApplyMap(MapIndex)) return;
                }
                appliedRevision = MapRevision;
                RPC_MapReady(MapRevision);
            }
            ApplyChessboardState(true);
            UpdateNextMapLabel();
            if (observedRound != RoundNumber)
            {
                observedRound = RoundNumber;
                hasBaseline = false;
                nextScoreRead = 0f;
            }
            if (observedState != State)
            {
                observedState = State;
                StateChanged?.Invoke(State);
                if (State == RoundState.Result)
                {
                    ResultsReady?.Invoke(GetResults());
                    nextScoreRead = 0f;
                }
            }
            if (State == RoundState.Result)
                spawns.SuspendPlayers();
            else if (State == RoundState.LoadingNextMap)
                spawns.ClearPlayers();
            else if (State == RoundState.Playing && MapRevision > 0 && appliedRevision == MapRevision)
                spawns.SpawnPlayersAtRandomPoints(RoundNumber);

            if (spawns.HasParticipated && !scoreReadPending && Time.unscaledTime >= nextScoreRead &&
                (State == RoundState.Playing || State == RoundState.Result))
            {
                nextScoreRead = Time.unscaledTime + 0.5f;
                _ = ReadLocalScore(RoundNumber);
            }
            if (ui != null)
                ui.Present(State, RoundNumber, PhaseTimer.RemainingTime(Runner) ?? 0f,
                    GetResults(), Runner.LocalPlayer.ToString(), appliedRevision == MapRevision);
        }

        private void UpdateNextMapLabel()
        {
            if (roundNextTextLabel == null || mapRoundIndex < 0 || displayedNextMapIndex == mapRoundIndex)
                return;

            if (mapCatalog == null)
                mapCatalog = MapCatalogSO.LoadDefault();

            string mapName = mapCatalog != null && mapCatalog.IsValidIndex(mapRoundIndex)
                ? mapCatalog.GetDisplayName(mapRoundIndex)
                : "찾을 수 없습니다 ㅠㅠ";

            roundNextTextLabel.SetText($"다음 맵: {mapName}");
            displayedNextMapIndex = mapRoundIndex;
        }

        private List<RoundResultEntry> GetResults() => Scores
            .OrderByDescending(pair => pair.Value.Score).ThenByDescending(pair => pair.Value.Kills)
            .ThenBy(pair => pair.Key.RawEncoded)
            .Select(pair => new RoundResultEntry(pair.Key.ToString(), pair.Value.Name.ToString(), pair.Value.Score, pair.Value.Kills))
            .ToList();

        private async Task ReadLocalScore(int round)
        {
            if (!ServiceLocator.TryGet<IDataStoreService>(out var data)) return;
            scoreReadPending = true;
            try
            {
                uint score = await data.GetAsync<uint>(nameof(PlayerDataStore.Score));
                uint kills = await data.GetAsync<uint>(nameof(PlayerDataStore.KillCount));
                string name = await data.GetAsync<string>(nameof(PlayerDataStore.PlayerName));
                if (this == null || Object == null || !Object.IsValid || round != RoundNumber) return;
                if (!hasBaseline)
                {
                    baselineScore = score;
                    baselineKills = kills;
                    hasBaseline = true;
                }
                localName = name;
                // Sample through the result interval to include already in-flight Cloud Save writes.
                RPC_ReportScore(round, localName ?? "Player", (int)Math.Min(int.MaxValue, score >= baselineScore ? score - baselineScore : 0u),
                    (int)Math.Min(int.MaxValue, kills >= baselineKills ? kills - baselineKills : 0u));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[RoundSystem] Could not read round score: {exception.Message}", this);
                nextScoreRead = Time.unscaledTime + 3f;
            }
            finally { scoreReadPending = false; }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_ReportScore(int round, string playerName, int score, int kills, RpcInfo info = default)
        {
            if (round != RoundNumber || (State != RoundState.Playing && State != RoundState.Result) ||
                !Runner.ActivePlayers.Contains(info.Source)) return;
            if (Scores.Count >= 64 && !Scores.ContainsKey(info.Source)) return;
            Scores.Set(info.Source, new RoundScore { Name = playerName.Length > 32 ? playerName.Substring(0, 32) : playerName, Score = Mathf.Max(0, score), Kills = Mathf.Max(0, kills) });
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_MapReady(int revision, RpcInfo info = default)
        {
            if (revision == MapRevision && Runner.ActivePlayers.Contains(info.Source) &&
                (MapReady.Count < 64 || MapReady.ContainsKey(info.Source))) MapReady.Set(info.Source, revision);
        }
    }
}
