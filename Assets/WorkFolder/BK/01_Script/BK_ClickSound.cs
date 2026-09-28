using UnityEngine;

public class BK_ClickSound : MonoBehaviour
{
  public void ClickSound()
  {
    BK_SoundManger.instance.SFXSoundOn.Invoke(SfxSoundEnum.Click);
  }
}
