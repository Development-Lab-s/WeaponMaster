using System;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace WorkFolder.PAP.Scripts.Sound
{
    public class SoundPlayer : NetworkBehaviour
    {
        [SerializeField] private AudioClip[] clips;
        private AudioSource _source;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
        }

        public async void PlaySound(AudioClip clip)
        {
            RpcPlaySound(Array.IndexOf(clips,clip));
            await Task.Delay(TimeSpan.FromSeconds(clip.length+1));
            Runner.Despawn(Object);
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RpcPlaySound(int id)
        {
            _source.clip = clips[id];
            _source.Play();
        }
    }
}