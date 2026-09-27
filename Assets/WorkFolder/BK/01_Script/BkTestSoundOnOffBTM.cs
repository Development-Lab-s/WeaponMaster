using UnityEngine;

public class BkTestSoundOnOffBTM : MonoBehaviour
{
  public void TestSound()
  {
    BK_SoundManger.instance.SFXSoundOn.Invoke(SfxSoundEnum.Sword);
  }
}
