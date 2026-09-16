using System.Threading.Tasks;

namespace WorkFolder.SUH.Scripts.CharacterVisual.UserInterface.ServiceLocator
{
    public interface IPlayerNameInterfaceChange
    {
        Task<bool> PlayerNameChange(string playerName);
    }
}