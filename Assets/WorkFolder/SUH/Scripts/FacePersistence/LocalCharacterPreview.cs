using System;
using DevLib.ServiceLocator;
using Unity.Services.Authentication;
using UnityEngine;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;

namespace WorkFolder.SUH.Scripts.FacePersistence
{
    // The weapon-selection render texture uses a separate, non-networked character.
    public sealed class LocalCharacterPreview : MonoBehaviour
    {
        [SerializeField] private SkinnedMeshRenderer meshRenderer;
        [SerializeField] private Material[] bodyMaterials;
        private CharacterFacePreview preview;
        private bool loading;
        private bool applied;
        private float retryAt;

        private void OnEnable()
        {
            applied = false;
            retryAt = 0f;
        }

        private async void Update()
        {
            if (loading || applied || Time.unscaledTime < retryAt) return;
            if (!ServiceLocator.TryGet(out IDataStoreService service)) return;
            loading = true;
            try
            {
                await UgsSignIn.EnsureSignedInAsync();
                if (this == null || !isActiveAndEnabled) return;
                uint bodyColor = await service.GetAsync<uint>(nameof(PlayerDataStore.BodyColor));
                if (this == null || !isActiveAndEnabled) return;
                if (meshRenderer == null || bodyMaterials == null || bodyColor >= bodyMaterials.Length)
                    throw new InvalidOperationException("Character preview body palette or renderer is missing.");

                preview ??= new CharacterFacePreview(meshRenderer);
                // Show the correct body immediately, even if fetching the face needs a retry.
                preview.Apply(bodyMaterials[bodyColor], null);
                var face = await FaceStore.Load(AuthenticationService.Instance.PlayerId);
                if (this == null || !isActiveAndEnabled) return;
                preview.Apply(bodyMaterials[bodyColor], face);
                applied = true;
            }
            catch (Exception ex)
            {
                if (this == null) return;
                retryAt = Time.unscaledTime + 10f;
                Debug.LogWarning($"Character preview load failed; retrying: {ex.Message}", this);
            }
            finally { loading = false; }
        }

        private void OnDestroy() => preview?.Dispose();
    }
}
