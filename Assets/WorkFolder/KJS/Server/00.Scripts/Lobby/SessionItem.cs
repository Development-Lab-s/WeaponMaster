using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _00.Scripts.Lobby
{
    public class SessionItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI roomNameText;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private Button joinButton;
        [SerializeField] private Image LockImage;

        private string _roomName;
        private bool _isPrivate;


        private void Awake()
        {
            LockImage.gameObject.SetActive(false);
        }

        public void SetSession( string roomName, int playerCount, int maxPlayers, bool isPrivate)
        {
            _roomName = roomName;
            _isPrivate = isPrivate;

            roomNameText.text = roomName;
            playerCountText.text = $"{playerCount}/{maxPlayers}";

            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(OnJoinButtonClicked);

            if (isPrivate)
            {
                LockImage.gameObject.SetActive(true);
            }
            
        }

        private void OnJoinButtonClicked()
        {
            if (_isPrivate)
            {
                LobbyManager.instance.OpenPasswordPopup(_roomName);
            }
            else
            {
                LobbyManager.instance.JoinRoom(_roomName);
            }
            
        }
    }
}