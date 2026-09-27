using System;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace WorkFolder.PAP.Scripts.Sound
{
    public class SoundPlayerLocal : MonoBehaviour
    {
        [SerializeField] private AudioClip[] clips;
        private AudioSource _source;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
        }

        public async void PlaySound(AudioClip clip)
        {
            PlaySound(Array.IndexOf(clips,clip));
            await Task.Delay(TimeSpan.FromSeconds(clip.length+1));
            Destroy(gameObject);
        }

        private void PlaySound(int id)
        {
            _source.clip = clips[id];
            _source.Play();
        }
    }
}