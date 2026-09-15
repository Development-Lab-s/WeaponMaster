using System;
using Fusion;
using TMPro;
using UnityEngine;

public class KillLog : NetworkBehaviour
{
   private TextMeshProUGUI killLogText;
   public static KillLog Instance;

   private void Awake()
   {
      if (Instance == null)
         Instance = this;
      else 
         Destroy(gameObject);
   }

   [Rpc(RpcSources.All , RpcTargets.All)]
   public void RPC_Kill([RpcTarget] PlayerRef target, NetworkString<_32> kill)
   {
      if (!(Runner.LocalPlayer == target)) return;
      foreach (GameObject v in KillLogPoolManger.instance.killLogList)
      {
         if (v.activeSelf == false)
         {
            v.SetActive(true);
            killLogText = v.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            killLogText.text = $"{kill}님을 처치!!!!";
            break;
         }
      }
   }
}
