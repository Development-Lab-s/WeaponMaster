using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class OffSetting : MonoBehaviour
{
   [SerializeField] private GameObject setting;
   private bool isSetting = false;

   private void Update()
   {
      if (Keyboard.current.escapeKey.wasPressedThisFrame)
      {
         setting.SetActive(!isSetting);
         isSetting = !isSetting;
      }
   }
}
