using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum SfxSoundEnum
{
  Sword,
  Lance,
  Walk,
  ServerEntry,
  GameStart,
  Equipment,
  Hit,
  Clash
}

public enum BGMSoundEnum
{
  EquipmentScene,
}

public class BK_SoundManger : MonoBehaviour
{
  [Header("SFXSound")]
  public AudioSource sfxSource;
  public AudioClip[] swordSoundClips;
  public AudioClip[] lanceSoundClips;
  public AudioClip[] walkSoundClips;
  public AudioClip[] serverEntrySoundClips;
  public AudioClip[] gameStartSoundClips;
  public AudioClip[] equipmentSoundClips;
  public AudioClip[] hitSoundClips;
  public AudioClip[] clashSoundClips;
  
  private SfxSoundEnum sfxSoundEnum;
  public Dictionary<SfxSoundEnum,AudioClip[]> sfxSoundClips =  new();
  public Action<SfxSoundEnum> SFXSoundOn;
  
  
  [Header("BGMSound")]
  public AudioSource bgmSource;
  public AudioClip[] equipmentClips;
  
  private BGMSoundEnum bgmSoundEnum;
  public Dictionary<BGMSoundEnum,AudioClip[]> bgmSoundClips =  new();
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
    sfxSoundClips[SfxSoundEnum.Sword] = swordSoundClips;
    sfxSoundClips[SfxSoundEnum.Lance] = lanceSoundClips;
    sfxSoundClips[SfxSoundEnum.Walk] = walkSoundClips;
    sfxSoundClips[SfxSoundEnum.ServerEntry] = serverEntrySoundClips;
    sfxSoundClips[SfxSoundEnum.GameStart] = gameStartSoundClips;
    sfxSoundClips[SfxSoundEnum.Equipment] = equipmentSoundClips;
    sfxSoundClips[SfxSoundEnum.Hit] = hitSoundClips;
    sfxSoundClips[SfxSoundEnum.Clash] = clashSoundClips;
    
    //bgm
    bgmSoundClips[BGMSoundEnum.EquipmentScene] =  equipmentClips;
  }

  public void OnEnable()
  {
    SFXSoundOn+=UpdateSfxSoundState;
    BGMSoundOn+=UpdateBGMSoundState;
  }

  public void UpdateSfxSoundState(SfxSoundEnum sfxSoundState)
  {
    if(sfxSource.isPlaying && sfxSoundEnum == sfxSoundState) return;
    sfxSoundEnum = sfxSoundState;
    AudioClip[] clips = sfxSoundClips[sfxSoundEnum];
    AudioClip clip = clips[Random.Range(0, clips.Length)];

    sfxSource.PlayOneShot(clip);
  }

  public void UpdateBGMSoundState(BGMSoundEnum bgmSoundState)
  {
    if (bgmSource.isPlaying && bgmSoundEnum == bgmSoundState)
    {
      return;
    }

    bgmSoundEnum = bgmSoundState;
    AudioClip[] clips = bgmSoundClips[bgmSoundEnum];
    AudioClip clip = clips[Random.Range(0, clips.Length)];
    bgmSource.clip = clip;
    bgmSource.loop = true;
    bgmSource.Play();
  }

}
