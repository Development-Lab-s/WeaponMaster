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

public enum BGMSoundEnum
{
  nomal,
  notNomal
}

public class BK_SoundManger : MonoBehaviour
{
  [Header("SFXSound")]
  private SfxSoundEnum sfxSoundEnum;
  public Dictionary<SfxSoundEnum,AudioClip[]> sfxSoundClips =  new Dictionary<SfxSoundEnum,AudioClip[]>();
  public AudioSource sfxSource;
  public AudioClip[] swordSoundClips;
  public AudioClip[] lanceSoundClips;
  public AudioClip[] uiSoundClips;
  public Action<SfxSoundEnum> SFXSoundOn;
  
  [Header("BGMSound")]
  private BGMSoundEnum bgmSoundEnum;
  public Dictionary<BGMSoundEnum,AudioClip[]> bgmSoundClips =  new Dictionary<BGMSoundEnum,AudioClip[]>();
  public AudioSource bgmSource;
  public AudioClip[] nomalClips;
  public AudioClip[] notNomalClips;
  public Action<BGMSoundEnum> BGMSoundOn;
  
  public static BK_SoundManger instance;
  
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
    
    //sfx
    sfxSoundClips[SfxSoundEnum.sword] = swordSoundClips;
    sfxSoundClips[SfxSoundEnum.lance] = lanceSoundClips;
    sfxSoundClips[SfxSoundEnum.ui] = uiSoundClips;
    
    //bgm
    bgmSoundClips[BGMSoundEnum.nomal] =  nomalClips;
    bgmSoundClips[BGMSoundEnum.notNomal] = notNomalClips;
  }

  public void OnEnable()
  {
    SFXSoundOn+=UpdateSfxSoundState;
    BGMSoundOn+=UpdateBGMSoundState;
  }

  public void UpdateSfxSoundState(SfxSoundEnum sound)
  {
    sfxSoundEnum = sound;
    AudioClip[] clips = sfxSoundClips[sfxSoundEnum];
    AudioClip clip = clips[Random.Range(0, clips.Length)];

    sfxSource.PlayOneShot(clip);
  }

  public void UpdateBGMSoundState(BGMSoundEnum sound)
  {
    if (bgmSource.isPlaying && bgmSoundEnum == sound)
    {
      return;
    }

    bgmSoundEnum = sound;
    AudioClip[] clips = bgmSoundClips[bgmSoundEnum];
    AudioClip clip = clips[Random.Range(0, clips.Length)];
    bgmSource.clip = clip;
    bgmSource.loop = true;
    bgmSource.Play();
  }

  /*public void SwordSound()
  {
    int index = Random.Range(0, swordSoundClips.Length);
    AudioClip clip = swordSoundClips[index];
    sfxSource.PlayOneShot(clip);
  }
  public void  LanceSound()
  {
    int index = Random.Range(0, lanceSoundClips.Length);
    AudioClip clip = lanceSoundClips[index];
    sfxSource.PlayOneShot(clip);
  }
  public void  UISound()
  {
    int index = Random.Range(0, uiSoundClips.Length);
    AudioClip clip = uiSoundClips[index];
    sfxSource.PlayOneShot(clip);
  }*/

}
