using System;
using System.Collections.Generic;
using DevLib.SoundSystem.Runtime;
using Fusion;
using UnityEngine;
using WorkFolder.PAP.Scripts.Sound;
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
  Clash,
  Swing,
  Crit,
  EntryBattle,
  Click,
  Impact,
  HitAss,
  Draw,
  Kill,
}

public enum BGMSoundEnum
{
  EquipmentScene,
  BattleScene,
}

public class BK_SoundManger : MonoBehaviour
{
  [Header("SFXSound")]
  public AudioClip[] swordSoundClips;
  public AudioClip[] lanceSoundClips;
  public AudioClip[] walkSoundClips;
  public AudioClip[] serverEntrySoundClips;
  public AudioClip[] gameStartSoundClips;
  public AudioClip[] equipmentSoundClips;
  public AudioClip[] hitSoundClips;
  public AudioClip[] clashSoundClips;
  public AudioClip[] swingSoundClips;
  public AudioClip[] critSoundClips;
  public AudioClip[] entrySoundClips;
  public AudioClip[] clickSoundClips;
  public AudioClip[] impactSoundClips;
  public AudioClip[] hitAssSoundClips;
  public AudioClip[] drawSoundClips;
  public AudioClip[] killSoundClips;
  
  
  private SfxSoundEnum sfxSoundEnum;
  [SerializeField] private SoundPlayer sfxPrefab;
  [SerializeField] private SoundPlayerLocal sfxPrefabLocal;
  public Dictionary<SfxSoundEnum,AudioClip[]> sfxSoundClips =  new();
  public Action<SfxSoundEnum> SFXSoundOn;
  
  
  [Header("BGMSound")]
  public AudioSource bgmSource;
  public AudioClip[] equipmentClips;
  public AudioClip[] battleClips;
  
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
    sfxSoundClips[SfxSoundEnum.Swing] = swingSoundClips;
    sfxSoundClips[SfxSoundEnum.Crit] = critSoundClips;
    sfxSoundClips[SfxSoundEnum.Click] = clickSoundClips;
    sfxSoundClips[SfxSoundEnum.EntryBattle] = entrySoundClips;
    sfxSoundClips[SfxSoundEnum.Impact] = impactSoundClips;
    sfxSoundClips[SfxSoundEnum.HitAss] = hitAssSoundClips;
    sfxSoundClips[SfxSoundEnum.Draw] = drawSoundClips;
    sfxSoundClips[SfxSoundEnum.Kill] = killSoundClips;
    
    
    //bgm
    bgmSoundClips[BGMSoundEnum.EquipmentScene] =  equipmentClips;
    bgmSoundClips[BGMSoundEnum.BattleScene] =  battleClips;
    
  }


  public void PlaySfx(SfxSoundEnum sfxSoundState, Vector3? position, NetworkRunner runner)
  {
    SoundPlayer soundPlayer = runner.Spawn(sfxPrefab, position);
    if (position == null)
      soundPlayer.GetComponent<AudioSource>().spatialBlend = 0f;
    sfxSoundEnum = sfxSoundState;
    AudioClip[] clips = sfxSoundClips[sfxSoundEnum];
    AudioClip clip = clips[Random.Range(0, clips.Length)];
    soundPlayer.PlaySound(clip);
  }

  public void PlaySfxLocal(SfxSoundEnum sfxSoundState, Vector3? position = null)
  {
    SoundPlayerLocal soundPlayer = Instantiate(sfxPrefabLocal);
    soundPlayer.transform.position = position ?? Vector3.zero;
    if (position == null)
      soundPlayer.GetComponent<AudioSource>().spatialBlend = 0f;
    sfxSoundEnum = sfxSoundState;
    AudioClip[] clips = sfxSoundClips[sfxSoundEnum];
    AudioClip clip = clips[Random.Range(0, clips.Length)];
    soundPlayer.PlaySound(clip);
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
