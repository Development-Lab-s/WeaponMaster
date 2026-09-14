using System;
using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestSpwandKillLog : MonoBehaviour
{
   private TextMeshProUGUI killLogText;
   [SerializeField] private string testKill;
   private void Update()
   {
      if (Keyboard.current.tKey.wasPressedThisFrame)
      {
         Spwan("테스트");
      }
   }

   [Rpc(RpcSources.All , RpcTargets.StateAuthority)]
   public void Spwan(string kill) //대충 사용할려면 킬로그 띄우고 싶을때 이 Spwan에 죽은 사람 이름 넘겨주면 됨
   {
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
