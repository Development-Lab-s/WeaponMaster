using System;
using UnityEngine;
using WorkFolder.KJS._01.Scripts;

public class BKDeadHealthCheck : MonoBehaviour
{
   [SerializeField]private HealthSystem healthSystem;
   private void Update()
   {
      if (healthSystem.Health <= 0)
      {
         
      }
   }
}
