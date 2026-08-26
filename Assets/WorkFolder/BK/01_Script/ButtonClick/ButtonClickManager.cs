using System;
using UnityEngine;

public class ButtonClickManager : MonoBehaviour
{
   public GameStartButton gameStartButton { get; private set; }
   public SettingButton settingButton { get; private set; }
   public QuitGameButton quitGameButton { get; private set; }

   private void Awake()
   {
      gameStartButton = GetComponentInChildren<GameStartButton>(true);
      settingButton = GetComponentInChildren<SettingButton>(true);
      quitGameButton = GetComponentInChildren<QuitGameButton>(true);
   }
}
