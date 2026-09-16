using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestKillLogVOne : MonoBehaviour
{
   [SerializeField]private TextMeshProUGUI killLogText;

   public void SetKillLogText(string kill)
   {
       killLogText.text = $"{kill}님을 죽였습니다!!!!";
   }
}
