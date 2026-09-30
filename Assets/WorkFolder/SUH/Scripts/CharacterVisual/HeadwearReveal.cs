using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

namespace WorkFolder.SUH.Scripts.CharacterVisual
{
    // Presentation only: each observer animates changes in the replicated/verified state.
    internal sealed class HeadwearReveal : IDisposable
    {
        private readonly GameObject target;
        private readonly Vector3 attachedPosition;
        private readonly Vector3 attachedScale;
        private readonly Quaternion attachedRotation;
        private readonly ParticleSystem particles;
        private Sequence sequence;
        private bool visible;

        public HeadwearReveal(GameObject target, Transform owner, Color color)
        {
            this.target = target;
            if (target == null) return;
            attachedPosition = target.transform.localPosition;
            attachedScale = target.transform.localScale;
            attachedRotation = target.transform.localRotation;
            target.SetActive(false);

            var effect = new GameObject(target.name + " Reveal Particles");
            effect.transform.SetParent(owner, false);
            particles = effect.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.6f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.055f);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.maxParticles = 32;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.enabled = false;
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            if (GraphicsSettings.currentRenderPipeline != null)
                renderer.sharedMaterial = GraphicsSettings.currentRenderPipeline.defaultParticleMaterial;
        }

        public void SetVisible(bool value)
        {
            if (target == null || visible == value) return;
            visible = value;
            sequence?.Kill();
            sequence = null;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Restore();
            target.SetActive(false);
            if (!value) return;

            Transform item = target.transform;
            // Offset in world metres, even when the imported head bone is rotated/scaled.
            Vector3 rise = item.parent != null ? item.parent.InverseTransformVector(Vector3.up * 0.3f) : Vector3.up * 0.3f;
            item.localPosition = attachedPosition + rise;
            item.localScale = Vector3.zero;
            particles.Play();
            for (int i = 0; i < 24; i++)
            {
                var burst = new ParticleSystem.EmitParams
                {
                    position = item.position + UnityEngine.Random.insideUnitSphere * 0.08f,
                    velocity = UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(0.15f, 0.5f)
                };
                particles.Emit(burst, 1);
            }

            sequence = DOTween.Sequence().SetUpdate(UpdateType.Late);
            sequence.AppendInterval(0.12f);
            sequence.AppendCallback(() => { if (target != null) target.SetActive(true); });
            sequence.Append(item.DOScale(attachedScale, 0.2f).SetEase(Ease.OutBack));
            sequence.Append(item.DOLocalMove(attachedPosition, 0.42f).SetEase(Ease.OutCubic));
            sequence.OnComplete(() => { Restore(); sequence = null; });
        }

        private void Restore()
        {
            if (target == null) return;
            target.transform.localPosition = attachedPosition;
            target.transform.localScale = attachedScale;
            target.transform.localRotation = attachedRotation;
        }

        public void Dispose()
        {
            sequence?.Kill();
            Restore();
            if (particles != null) UnityEngine.Object.Destroy(particles.gameObject);
        }
    }
}
