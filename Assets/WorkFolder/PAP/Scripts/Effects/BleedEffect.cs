using System.Collections;
using Fusion;
using UnityEngine;

namespace WorkFolder.PAP.Scripts.Effects
{
    public class BleedEffect : MonoBehaviour
    {
        private ParticleSystem particleSystem;

        public void SetPositionAndPlayEffect(Vector3 position, Quaternion rotation)
        {
            particleSystem.transform.position = position;
            particleSystem.transform.rotation = rotation;
            particleSystem.Play();
            StartCoroutine(DestroyEffectRoutine());
        }

        private IEnumerator DestroyEffectRoutine()
        {
            yield return new WaitForSeconds(particleSystem.main.duration + 1);
            Destroy(gameObject);
        }
    }
}