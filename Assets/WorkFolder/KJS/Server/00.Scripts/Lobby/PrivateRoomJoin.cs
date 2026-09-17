using TMPro;
using UnityEngine;

namespace _00.Scripts.Lobby
{
    public class PrivateRoomJoin : MonoBehaviour
    {
        [SerializeField] private TMP_InputField passwordInput;
        private string _roomName;
        
        public void Open(string roomName)
        {
            _roomName = roomName;

            passwordInput.text = "";

            gameObject.SetActive(true);
            passwordInput.ActivateInputField();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        public void Confirm()
        {
            string password = passwordInput.text.Trim();

            if (string.IsNullOrEmpty(password))
            {
                return;
            }

            LobbyManager.instance.TryJoinPrivateRoom( _roomName, password );
        }
    }
}