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
        [SerializeField] private GameObject killObject;
        private NetworkObject _obj;
        public override void Spawned()
        {
            _obj = GetComponent<NetworkObject>();
        }

        public void TryReset(string deathMent = "죽음!")
        {
            if (!HasStateAuthority) return;
            foreach (MonoBehaviour behaviour in _obj.GetComponentsInChildren<MonoBehaviour>())
            {
                behaviour.enabled = false;
            }

            foreach (Rigidbody rb in _obj.GetComponentsInChildren<Rigidbody>())
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.gameObject.layer = LayerMask.NameToLayer("Died");
                rb.gameObject.tag = "Untagged";
            }
            Destroy(killObject);
            UIManager.Instance.DropDeathUI(deathMent);
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
                Owner.GetModule<IHealthSystem>().Die(resetMent[Random.Range(0, resetMent.Length)]);
            }
        }
    }
}