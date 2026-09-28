using System;
using System.Collections;
using System.Collections.Generic;
using DevLib.ServiceLocator;
using Fusion;
using UnityEngine;
using WorkFolder.SUH.Scripts.RoundSystem.Lib;
using WorkFolder.SUH.Scripts.RoundSystem.Lib.Interface;

namespace WorkFolder.SUH.Scripts.RoundSystem
{
    [RequireComponent(typeof(NetworkObject))]
    public class RoundSystem : NetworkBehaviour, IRoundSystem
    {
        [Header("Round Settings")]
        [SerializeField, Min(1f)] private float roundDurationSeconds = 120f;
        [SerializeField, Min(0f)] private float resultDurationSeconds = 5f;
        [SerializeField, Min(1)] private int resultCount = 3;

        [Networked] public RoundState State { get; private set; } = RoundState.Waiting;
        [Networked] public int RoundNumber { get; private set; }
        [Networked] private TickTimer RoundTimer { get; set; }

        private Coroutine mapTransitionCoroutine;

        public event Action<RoundState> StateChanged;
        public event Action<IReadOnlyList<RoundResultEntry>> ResultsReady;

        private bool HasRoundAuthority =>
            HasStateAuthority && (Runner.IsServer || Runner.IsSharedModeMasterClient);

        private void Awake()
        {
            ServiceLocator.Register<IRoundSystem>(this);
        }

        private void OnDestroy()
        {
            if (mapTransitionCoroutine != null)
                StopCoroutine(mapTransitionCoroutine);

            ServiceLocator.UnRegister<IRoundSystem>(this);
        }

        public override void Spawned()
        {
            if (HasRoundAuthority)
                StartRound();
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasRoundAuthority || State != RoundState.Playing)
                return;

            if (RoundTimer.Expired(Runner))
                EndRound();
        }

        public void StartRound()
        {
            if (!HasRoundAuthority)
                return;

            RoundNumber++;
            ChangeState(RoundState.Playing);
            RoundTimer = TickTimer.CreateFromSeconds(Runner, roundDurationSeconds);

            if (ServiceLocator.TryGet<IRoundScoreService>(out var scoreService))
                scoreService.ResetRoundScores();
        }

        public void EndRound()
        {
            if (!HasRoundAuthority || State != RoundState.Playing)
                return;

            ChangeState(RoundState.Result);

            IReadOnlyList<RoundResultEntry> results = Array.Empty<RoundResultEntry>();
            if (ServiceLocator.TryGet<IRoundScoreService>(out var scoreService))
                results = scoreService.GetRanking(resultCount);

            ResultsReady?.Invoke(results);

            if (ServiceLocator.TryGet<IRoundResultUI>(out var resultUI))
                resultUI.ShowResults(results);

            mapTransitionCoroutine = StartCoroutine(LoadNextMapAfterResult());
        }

        private IEnumerator LoadNextMapAfterResult()
        {
            yield return new WaitForSeconds(resultDurationSeconds);

            ChangeState(RoundState.LoadingNextMap);

            if (ServiceLocator.TryGet<IMapService>(out var mapService))
            {
                if (mapService.LoadNextMap())
                {
                    while (mapService.IsLoading)
                        yield return null;
                }
            }

            if (ServiceLocator.TryGet<ISpawnService>(out var spawnService))
                spawnService.SpawnPlayersAtRandomPoints(RoundNumber);

            if (ServiceLocator.TryGet<IRoundResultUI>(out var resultUI))
                resultUI.HideResults();

            StartRound();
            mapTransitionCoroutine = null;
        }

        private void ChangeState(RoundState nextState)
        {
            if (State == nextState)
                return;

            State = nextState;
            StateChanged?.Invoke(nextState);
        }
    }
}
