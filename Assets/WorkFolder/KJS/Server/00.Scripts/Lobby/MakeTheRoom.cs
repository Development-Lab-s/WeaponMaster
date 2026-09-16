using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace _00.Scripts.Lobby
{
    public class MakeTheRoom : MonoBehaviour
    {
        [SerializeField] private TMP_InputField password;
        [SerializeField] private TMP_InputField roomName;
       

        public void OnMakePublic()
        {
            string roomName = password.text.Trim();

            if (string.IsNullOrEmpty(roomName))
            {
                Debug.Log("방 이름을 입력해주세요.");
                return;
            }
            

            LobbyManager.instance.MakePublic(roomName);
        }

        public void OnMakePrivate()
        {
            string _password = password.text.Trim();
            string _roomName = roomName.text.Trim();

            if (string.IsNullOrEmpty(_password))
            {
                Debug.Log("비밀번호를 입력해주세요.");
                return;
            }
            
            if (string.IsNullOrEmpty(_roomName))
            {
                Debug.Log("방 이름을 입력해주세요.");
                return;
            }

            LobbyManager.instance.MakePrivate(_roomName, _password);
        }
    }
}
//if (string.IsNullOrEmpty(inputField.text) || inputField.text == "     " ||  inputField.text == "    " ||  inputField.text == "   " ||  inputField.text == "   " ||  inputField.text == "  " ||  inputField.text == " ")