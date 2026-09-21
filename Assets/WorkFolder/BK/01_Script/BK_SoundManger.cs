using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum SfxSoundEnum
{
  sword,
  lance,
  ui
}

public class BK_SoundManger : MonoBehaviour
{
  public SfxSoundEnum sfxSoundEnum;
  
  public Dictionary<SfxSoundEnum,AudioClip[]> sfxSoundClips;

  public static BK_SoundManger instance;
  public AudioSource source;
  public AudioClip[] swordSoundClips;
  public AudioClip[] lanceSoundClips;
  public AudioClip[] uiSoundClips;

  public Action SoundState;
  private void Awake()
  {
    if (instance == null)
    {
      instance = this;
    }
    else
    {
      Destroy(gameObject);
      return;
    }
  }

  private void Start()
  {
    sfxSoundClips[SfxSoundEnum.sword] = swordSoundClips;
    sfxSoundClips[SfxSoundEnum.lance] = lanceSoundClips;
    sfxSoundClips[SfxSoundEnum.ui] = uiSoundClips;
  }

  public void UpdateSfxSoundState(SfxSoundEnum sound)
  {
    sfxSoundEnum = sound;
    AudioClip[] clips = sfxSoundClips[sfxSoundEnum];
    AudioClip clip = clips[Random.Range(0, clips.Length)];

    source.PlayOneShot(clip);
  }

  public void SwordSound()
  {
    int index = Random.Range(0, swordSoundClips.Length);
    AudioClip clip = swordSoundClips[index];
    source.PlayOneShot(clip);
  }
  public void  LanceSound()
  {
    int index = Random.Range(0, lanceSoundClips.Length);
    AudioClip clip = lanceSoundClips[index];
    source.PlayOneShot(clip);
  }
  public void  UISound()
  {
    int index = Random.Range(0, uiSoundClips.Length);
    AudioClip clip = uiSoundClips[index];
    source.PlayOneShot(clip);
  }

}
