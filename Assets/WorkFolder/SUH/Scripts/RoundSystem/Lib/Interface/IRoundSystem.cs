using System;
using System.Collections.Generic;

namespace WorkFolder.SUH.Scripts.RoundSystem.Lib.Interface
{
    public interface IRoundSystem
    {
        event Action<RoundState> StateChanged;
        event Action<IReadOnlyList<RoundResultEntry>> ResultsReady;

        RoundState State { get; }
        int RoundNumber { get; }

        void StartRound();
        void EndRound();
    }
}
