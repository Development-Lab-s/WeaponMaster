using System.Threading.Tasks;

namespace WorkFolder.SUH.Scripts.Leaderboard.Lib.Interface.ServiceLocator
{
    public interface ILeaderboardSender
    {
        Task TryUpdatePlayer();
    }
}