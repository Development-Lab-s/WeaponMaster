using System;
using UnityEngine;

namespace WorkFolder.SUH.Scripts.FacePersistence
{
    // Owns only the preview face material/texture, never edits palette assets.
    public sealed class CharacterFacePreview : IDisposable
    {
        private readonly SkinnedMeshRenderer renderer;
        private readonly Material[] originals;
        private Material faceMaterial;
        private Texture2D texture;

        public CharacterFacePreview(SkinnedMeshRenderer renderer)
        {
            this.renderer = renderer;
            originals = renderer.sharedMaterials;
        }

        public void Apply(Material skin, SavedFace saved)
        {
            if (originals.Length < 2) throw new InvalidOperationException("Character preview requires body and face slots.");
            if (faceMaterial == null) faceMaterial = new Material(skin) { name = "Character Editor Face" };
            faceMaterial.shader = skin.shader;
            faceMaterial.CopyPropertiesFromMaterial(skin);
            if (texture == null) texture = new Texture2D(512, 512, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var color = FaceDrawShared.GetColor(skin);
            if (saved != null)
            {
                FaceDrawShared.ProjectFaceUV(renderer, 1);
                if (!texture.LoadImage(saved.Decode())) throw new InvalidOperationException("Preview face PNG decode failed.");
                FaceDrawShared.ApplySkinBackground(texture, saved.StoredBackground, color);
            }
            else
            {
                Color32 fill = color;
                fill.a = 255;
                var pixels = new Color32[texture.width * texture.height];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = fill;
                texture.SetPixels32(pixels);
                texture.Apply(false);
            }
            FaceDrawShared.ApplyTexture(faceMaterial, texture);
            var materials = renderer.sharedMaterials;
            materials[0] = skin;
            materials[1] = faceMaterial;
            renderer.sharedMaterials = materials;
        }

        public void Dispose()
        {
            if (renderer != null) renderer.sharedMaterials = originals;
            if (Application.isPlaying)
            {
                if (faceMaterial != null) UnityEngine.Object.Destroy(faceMaterial);
                if (texture != null) UnityEngine.Object.Destroy(texture);
            }
            else
            {
                if (faceMaterial != null) UnityEngine.Object.DestroyImmediate(faceMaterial);
                if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
