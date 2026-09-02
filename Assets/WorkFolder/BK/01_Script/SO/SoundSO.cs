using System;
using UnityEngine;

namespace BK._01_Script.SO
{
    [CreateAssetMenu(fileName = "FILENAME", menuName = "SoundSO", order = 0)]
    public class SoundSO : ScriptableObject
    {
        [SerializeField, Range(0f, 1f)]
        private float masterVolume = 1f;

        [SerializeField, Range(0f, 1f)]
        private float bgmVolume = 1f;

        [SerializeField, Range(0f, 1f)]
        private float vfxVolume = 1f;

        public float MasterVolume
        {
            get => masterVolume;
            set => masterVolume = Mathf.Clamp01(value);
        }

        public float BgmVolume
        {
            get => bgmVolume;
            set => bgmVolume = Mathf.Clamp01(value);
        }

        public float VfxVolume
        {
            get => vfxVolume;
            set => vfxVolume = Mathf.Clamp01(value);
        }
        //태헌님의 말 -> 감도만 넣어놓으면 될거 같다 -> 감도에 있는 값 SO에 저장하는 코드
    }
}