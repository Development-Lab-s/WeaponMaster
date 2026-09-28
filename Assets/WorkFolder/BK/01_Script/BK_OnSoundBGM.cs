using System;
using UnityEngine;

public class BK_OnSoundBGM : MonoBehaviour
{
  public void Sound()
  {
    BK_SoundManger.instance.BGMSoundOn.Invoke(BGMSoundEnum.EquipmentScene); 
  }

  private void Start()
  {
    Sound();
  }
}
