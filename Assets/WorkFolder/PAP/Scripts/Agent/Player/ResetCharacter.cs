using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using WorkFolder.PAP.Scripts.MainSystems;

namespace WorkFolder.PAP.Scripts.Agent.Player
{
    public class ResetCharacter : NetworkBehaviour
    {
        private NetworkObject _obj;
        public override void Spawned()
        {
            _obj = GetComponent<NetworkObject>();
        }

        public void TryReset()
        {
            if (!HasStateAuthority) return;
            Runner.Despawn(_obj);
            SelectStartUI.Instance.OpenSelectUI();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        //테스트코드

        private void Update()
        {
            if (!HasStateAuthority) return;
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                TryReset();
            }
        }
    }
}