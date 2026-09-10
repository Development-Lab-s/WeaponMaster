using TMPro;
using UnityEngine;

namespace _00.Scripts.Lobby
{
    public class PasswordPopup : MonoBehaviour
    {
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private TextMeshProUGUI errorText;

        private string _roomName;

        private void Awake()
        {
            gameObject.SetActive(false);
        }

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

        public void CheckPassword()
        {
            string password = passwordInput.text.Trim();

            if (string.IsNullOrEmpty(password))
            {
                errorText.text = "비밀번호를 입력해주세요.";
                return;
            }

            LobbyManager.instance.TryJoinPrivateRoom(
                _roomName,
                password
            );
        }

        public void ShowError()
        {
            errorText.text = "비밀번호가 틀렸습니다.";
        }
    }
}