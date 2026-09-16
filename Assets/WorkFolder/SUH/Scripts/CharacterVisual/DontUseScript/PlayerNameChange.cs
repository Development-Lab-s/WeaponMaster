using System.Threading.Tasks;
using DevLib.ServiceLocator;
using UnityEngine;
using WorkFolder.SUH.Scripts.CharacterVisual.UserInterface.ServiceLocator;

namespace WorkFolder.SUH.Scripts.CharacterVisual.CharacterVisual
{
    public class PlayerNameChange : MonoBehaviour
    {


        public async Task<bool> SelectName(string PlayerName)
        {


            
            return true;
        }

        public async void CheckingName(string PlayerName)
        {
            SelectedName(PlayerName);
        }

        private void SelectedName(string playerName)
        {
            IPlayerNameInterfaceChange service = ServiceLocator.Get<IPlayerNameInterfaceChange>();
            
            service.PlayerNameChange(playerName);
        }
    }
}