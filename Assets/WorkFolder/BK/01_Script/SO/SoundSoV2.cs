using UnityEngine;

[CreateAssetMenu(fileName = "SoundSoV2", menuName = "BKSO/SoundSoV2")]
public class SoundSoV2 : ScriptableObject
{
    [Header("Sound")]
    [Range(0f, 1f)] public float MasterVolume = 1f;
    [Range(0f, 1f)] public float BGMVolume = 1f;
    [Range(0f, 1f)] public float SFXVolume = 1f;
}
