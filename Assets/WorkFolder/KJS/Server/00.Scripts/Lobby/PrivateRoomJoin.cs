using TMPro;
using UnityEngine;

namespace _00.Scripts.Lobby
{
    public class PrivateRoomJoin : MonoBehaviour
    {
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private TextMeshProUGUI errorText;
        private string _roomName;
        
        public void Open(string roomName)
        {
            _roomName = roomName;

            passwordInput.text = "";
            errorText.text = "";

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
                errorText.text = "password is empty.";
                return;
            }

            LobbyManager.instance.TryJoinPrivateRoom( _roomName, password );
        }

        public void ShowError()
        {
            errorText.text = "password is incorrect.";
        }
    }
}