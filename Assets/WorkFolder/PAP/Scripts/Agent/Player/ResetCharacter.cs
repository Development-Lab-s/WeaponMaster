using DevLib.ModuleSystem;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using WorkFolder.KJS._01.Scripts;
using WorkFolder.PAP.Scripts.MainSystems;

namespace WorkFolder.PAP.Scripts.Agent.Player
{
    public class ResetCharacter : NetworkModule
    {
        private NetworkObject _obj;

        private string[] targetByRando =
        {
            "의문",
            "없음",
            "데브랩 개발자 일동",
            "의도된 게임 디자인",
            "데브랩은 2분기에서 끝이다!"
        };
        public override void Spawned()
        {
            _obj = GetComponent<NetworkObject>();
        }

        public void TryReset(string deathMent = "죽음!", string targetBy = "")
        {
            if (!HasStateAuthority) return;
            if (targetBy == "")
                targetBy = targetByRando[Random.Range(0, targetByRando.Length)];
            UIManager.Instance.DropDeathUI(deathMent, targetBy);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        //테스트코드

        private string[] resetMent = { "삶에서 도피하다!", "죽음과 포옹하다!", "기압으로 인해 죽음!", "초보는 장비탓을 한다!", "가장 멍청한 죽음!"};

        private void Update()
        {
            if (!HasStateAuthority) return;
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                Owner.GetModule<IHealthSystem>().Die(resetMent[Random.Range(0, resetMent.Length)],"");
            }
        }
    }
}