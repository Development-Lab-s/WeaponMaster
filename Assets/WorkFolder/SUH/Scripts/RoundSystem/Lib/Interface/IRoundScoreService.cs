using System.Collections.Generic;

namespace WorkFolder.SUH.Scripts.RoundSystem.Lib.Interface
{
    public interface IRoundScoreService
    {
        IReadOnlyList<RoundResultEntry> GetRanking(int count);
        void ResetRoundScores();
    }
}
