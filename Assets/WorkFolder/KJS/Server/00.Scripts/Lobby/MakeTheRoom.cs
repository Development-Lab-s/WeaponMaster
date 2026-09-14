using System;
using TMPro;
using UnityEngine;

namespace _00.Scripts.Lobby
{
    public class MakeTheRoom : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputField;

        public void OnMakePublic()
        {
            string roomName = inputField.text.Trim();

            if (string.IsNullOrEmpty(roomName))
            {
                Debug.Log("방 이름을 입력해주세요.");
                return;
            }

            LobbyManager.instance.MakePublic(roomName);
        }

        public void OnMakePrivate()
        {
            string roomName = inputField.text.Trim();

            if (string.IsNullOrEmpty(roomName))
            {
                Debug.Log("방 이름을 입력해주세요.");
                return;
            }

            LobbyManager.instance.MakePrivate(roomName, roomName);
        }
    }
}
//if (string.IsNullOrEmpty(inputField.text) || inputField.text == "     " ||  inputField.text == "    " ||  inputField.text == "   " ||  inputField.text == "   " ||  inputField.text == "  " ||  inputField.text == " ")