namespace WorkFolder.SUH.Scripts.Leaderboard.Lib.Interface.ServiceLocator
{
    public interface ILeaderboardLocalMaker
    {
        void AddScore(uint KillCount);
        void GetTopRanker(ushort RankerCount);
    }
}