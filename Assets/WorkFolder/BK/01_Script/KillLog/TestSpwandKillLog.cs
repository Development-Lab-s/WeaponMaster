using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestSpwandKillLog : MonoBehaviour
{
   [SerializeField]private GameObject killLog;
   [SerializeField]private Transform parent;
   private void Update()
   {
      if (Keyboard.current.tKey.wasPressedThisFrame)
      {
         Spwan();
      }
   }

   private void Spwan()
   {
      Instantiate(killLog, parent);
   }
}
