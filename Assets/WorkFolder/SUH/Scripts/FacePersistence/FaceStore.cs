using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models.Data.Player;
using UnityEngine;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using SaveOptions = Unity.Services.CloudSave.Models.Data.Player.SaveOptions;

namespace WorkFolder.SUH.Scripts.FacePersistence
{
    // Cloud Save uses Newtonsoft, which traverses Color.linear/gamma recursively.
    // Only primitive channels belong in the persisted DTO.
    [Serializable]
    public struct FaceColorData
    {
        public float r, g, b, a;
        public static implicit operator FaceColorData(Color color) =>
            new FaceColorData { r = color.r, g = color.g, b = color.b, a = color.a };
        public static implicit operator Color(FaceColorData color) => new Color(color.r, color.g, color.b, color.a);
    }

    [Serializable]
    public sealed class SavedFace
    {
        public int Version = 2;
        public uint BodyColor;
        public FaceColorData BackgroundColor = Color.white;
        public string Png;
        public byte[] Decode()
        {
            if ((Version != 1 && Version != 2) || string.IsNullOrEmpty(Png) || Png.Length > 1600000)
                throw new InvalidOperationException("Invalid saved face.");
            byte[] bytes = Convert.FromBase64String(Png);
            // Check the PNG header before allocating a texture from cloud data.
            if (bytes.Length < 24 || bytes[0] != 137 || bytes[1] != 80 || bytes[2] != 78 || bytes[3] != 71
                || bytes[16] != 0 || bytes[17] != 0 || bytes[18] != 2 || bytes[19] != 0
                || bytes[20] != 0 || bytes[21] != 0 || bytes[22] != 2 || bytes[23] != 0)
                throw new InvalidOperationException("Expected a 512x512 face PNG.");
            return bytes;
        }

        [Newtonsoft.Json.JsonIgnore]
        public Color StoredBackground => Version == 1 ? Color.white : (Color)BackgroundColor;
    }

    // Separate key: score/name updates must not re-upload the face image.
    public static class FaceStore
    {
        public const string Key = "FaceAppearanceV1";
        private static readonly Dictionary<string, Task<SavedFace>> Cache = new();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Cache.Clear();
        public static void Invalidate(string playerId)
        {
            if (!string.IsNullOrEmpty(playerId)) Cache.Remove(playerId);
        }

        public static async Task<SavedFace> Load(string playerId)
        {
            if (!Cache.TryGetValue(playerId, out var task))
                Cache[playerId] = task = Fetch(playerId);
            try { return await task; }
            catch { Cache.Remove(playerId); throw; }
        }

        private static async Task<SavedFace> Fetch(string playerId)
        {
            await UgsSignIn.EnsureSignedInAsync();
            var data = await CloudSaveService.Instance.Data.Player.LoadAsync(
                new HashSet<string> { Key }, new LoadOptions(new PublicReadAccessClassOptions(playerId)));
            return data.TryGetValue(Key, out var item) ? item.Value.GetAs<SavedFace>() : null;
        }

        public static async Task Save(uint bodyColor, Texture2D texture, Color backgroundColor)
        {
            var face = new SavedFace { BodyColor = bodyColor, BackgroundColor = backgroundColor, Png = Convert.ToBase64String(texture.EncodeToPNG()) };
            face.Decode();
            await UgsSignIn.EnsureSignedInAsync();
            string owner = AuthenticationService.Instance.PlayerId;
            await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object> { { Key, face } },
                new SaveOptions(new PublicWriteAccessClassOptions()));
            Cache[owner] = Task.FromResult(face);
        }
    }
}
