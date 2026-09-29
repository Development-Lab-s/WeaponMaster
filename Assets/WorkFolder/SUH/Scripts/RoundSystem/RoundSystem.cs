using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevLib.ServiceLocator;
using Fusion;
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

    [RequireComponent(typeof(NetworkObject), typeof(MapService), typeof(SpawnService))]
    public sealed class RoundSystem : NetworkBehaviour, IRoundSystem
    {
        [SerializeField, Min(1f)] private float roundDurationSeconds = 284f;
        [SerializeField, Min(10f)] private float resultDurationSeconds = 10f;
        [SerializeField, Min(1f)] private float mapReadyTimeoutSeconds = 15f;
        [SerializeField] private RoundResultUI ui;
        [Networked] public RoundState State { get; private set; }
        [Networked] public int RoundNumber { get; private set; }
        [Networked] private TickTimer PhaseTimer { get; set; }
        [Networked] private int MapIndex { get; set; }
        [Networked] private int MapRevision { get; set; }
        [Networked] private int ChessSeed { get; set; }
        [Networked] private int FallenTileCount { get; set; }
        [Networked] private double ChessRoundStart { get; set; }
        [Networked] private int ChessCycleNumber { get; set; }
        private const float ChessCycleSeconds = 20f;
        private const float ChessFirstFallSeconds = 2f;
        private const float ChessRestoreSeconds = 12f;
        internal const float TileFallInterval = 0.5f;
        internal const int TilesPerFall = 4;
        [Networked, Capacity(64)] private NetworkDictionary<PlayerRef, RoundScore> Scores => default;
        [Networked, Capacity(64)] private NetworkDictionary<PlayerRef, int> MapReady => default;

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
        private bool HasRoundAuthority => Object != null && Object.IsValid && HasStateAuthority;

        private void Awake()
        {
            maps = GetComponent<MapService>();
            spawns = GetComponent<SpawnService>();
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
                        StartRound();
                    break;
                case RoundState.Playing:
                    if (PhaseTimer.Expired(Runner)) EndRound();
                    else UpdateChessboardCycle();
                    break;
                case RoundState.Result:
                    if (!PhaseTimer.Expired(Runner)) break;
                    int next = maps.ChooseNextMap(MapIndex);
                    if (next < 0)
                    {
                        Debug.LogError("[RoundSystem] No rotation maps registered in MapCatalog. Retrying in 10 seconds.", this);
                        PhaseTimer = TickTimer.CreateFromSeconds(Runner, 10f);
                        break;
                    }
                    MapIndex = next;
                    MapRevision++;
                    FallenTileCount = 0;
                    ChessCycleNumber = 0;
                    MapReady.Clear();
                    State = RoundState.LoadingNextMap;
                    PhaseTimer = TickTimer.CreateFromSeconds(Runner, mapReadyTimeoutSeconds);
                    Runner.SessionInfo.UpdateCustomProperties(new Dictionary<string, SessionProperty>
                    {
                        [MapCatalogSO.SessionPropertyKey] = next
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
            double elapsed = Math.Max(0d, Runner.SimulationTime - ChessRoundStart);
            int cycle = (int)(elapsed / ChessCycleSeconds);
            if (ChessCycleNumber != cycle)
            {
                ChessCycleNumber = cycle;
                ChessSeed = UnityEngine.Random.Range(1, int.MaxValue);
            }
            double phase = elapsed - cycle * ChessCycleSeconds;
            // Restore at 12s, then leave a safe interval before the next collapse.
            // Six complete collapse/restore cycles occur in a 120-second round.
            int limit = Mathf.Max(1, Mathf.FloorToInt(maps.ChessTileCount * 0.75f));
            FallenTileCount = phase < ChessFirstFallSeconds || phase >= ChessRestoreSeconds ? 0 :
                Math.Min(limit, ((int)((phase - ChessFirstFallSeconds) / TileFallInterval) + 1) * TilesPerFall);
            ApplyChessboardState();
        }

        private void ApplyChessboardState(bool rendering = false)
        {
            if (appliedRevision != MapRevision) return;
            double clock = rendering ? Runner.LocalRenderTime : Runner.SimulationTime;
            float fallAge = (float)(clock - ChessRoundStart -
                ChessCycleNumber * ChessCycleSeconds - ChessFirstFallSeconds);
            maps.ApplyChessTiles(ChessSeed, FallenTileCount, fallAge);
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
