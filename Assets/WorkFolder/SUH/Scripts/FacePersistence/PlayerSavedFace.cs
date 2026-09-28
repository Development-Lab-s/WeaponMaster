using System;
using UnityEngine;
using WorkFolder.PAP.Scripts.Agent.Player;

namespace WorkFolder.SUH.Scripts.FacePersistence
{
    // Fusion already replicates Player.playerId in Shared Mode, including to late joiners.
    // Resolve that owner's public face once per client instead of broadcasting PNG RPC chunks.
    public sealed class PlayerSavedFace : MonoBehaviour
    {
        private Player player;
        private string loadedId;
        private bool loading;
        private float retryAt;
        private Material material;
        private Material original;
        private Texture2D texture;
        private SavedFace loadedFace;
        private Color appliedSkin;
        private void Awake() => player = GetComponent<Player>();

        private async void Update()
        {
            if (loading || Time.unscaledTime < retryAt || player == null || player.Object == null || !player.Object.IsValid) return;
            string id = player.playerId.ToString();
            // Spawned loads the owner's body color asynchronously. Alpha remains zero until set.
            if (string.IsNullOrEmpty(id) || player.NetWorkedColor.a <= 0f) return;
            loading = true;
            try
            {
                if (id == loadedId)
                {
                    if (loadedFace != null && !appliedSkin.Equals(player.NetWorkedColor))
                        Apply(loadedFace, player.NetWorkedColor);
                    return;
                }
                var saved = await FaceStore.Load(id);
                if (this == null || player.Object == null || !player.Object.IsValid) return;
                if (player.playerId.ToString() != id) return;
                if (saved != null) Apply(saved, player.NetWorkedColor);
                loadedFace = saved;
                loadedId = id;
            }
            catch (Exception ex)
            {
                retryAt = Time.unscaledTime + 10f;
                Debug.LogWarning($"Face load failed; retrying: {ex.Message}", this);
            }
            finally { loading = false; }
        }

        private void Apply(SavedFace saved, Color currentSkin)
        {
            var renderer = player.MeshRenderer;
            if (renderer == null || renderer.sharedMaterials.Length <= 1)
                throw new InvalidOperationException("Player face material slot 1 is missing.");
            // Prepare UVs before committing a material, including when retrying a failed load.
            FaceDrawShared.ProjectFaceUV(renderer, 1);
            if (texture == null) texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            if (!texture.LoadImage(saved.Decode())) throw new InvalidOperationException("Face PNG decode failed.");
            // Always recompose from the original PNG, never from an already recolored texture.
            FaceDrawShared.ApplySkinBackground(texture, saved.StoredBackground, currentSkin);
            if (material == null)
            {
                var materials = renderer.sharedMaterials;
                original = materials[1];
                material = new Material(original) { name = "Saved Player Face" };
                materials[1] = material;
                renderer.sharedMaterials = materials;
            }
            FaceDrawShared.ApplyTexture(material, texture);
            appliedSkin = currentSkin;
        }

        private void OnDestroy()
        {
            FaceStore.Invalidate(loadedId);
            if (material != null && player != null && player.MeshRenderer != null)
            {
                var materials = player.MeshRenderer.sharedMaterials;
                if (materials.Length > 1 && materials[1] == material) { materials[1] = original; player.MeshRenderer.sharedMaterials = materials; }
            }
            if (material != null) Destroy(material);
            if (texture != null) Destroy(texture);
        }
    }
}
