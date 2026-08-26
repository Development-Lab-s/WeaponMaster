using System;
using UnityEngine;

public class OnSettingAndOnGoTOMainMenuButton : MonoBehaviour
{
   [SerializeField]private GameObject setting;
   [SerializeField]private GameObject mainMenu;
   private bool isOnOff = false;

   private void Start()
   {
      setting.SetActive(isOnOff);
      mainMenu.SetActive(isOnOff);
   }

   public void OnOffButton()
   {
      setting.SetActive(!isOnOff);
      mainMenu.SetActive(!isOnOff);
      isOnOff = !isOnOff;
   }
}
