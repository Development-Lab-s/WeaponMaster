using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WorkFolder.PAP.Scripts.Agent.Player;
using WorkFolder.SUH.Scripts.FacePersistence;

internal static class FacePersistenceValidation
{
    [MenuItem("Tools/Face Persistence/Validate Setup")]
    private static void Validate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var scene = EditorSceneManager.OpenPreviewScene("Assets/WorkFolder/SUH/Scene/FaceDraw.unity");
        Texture2D input = null, output = null;
        Mesh projected = null;
        GameObject instance = null;
        try
        {
            FaceSaveController controller = null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponentInChildren<FaceSaveController>(true) is { } found) controller = found;
            if (controller == null) throw new Exception("FaceSaveController missing in FaceDraw scene.");
            var serialized = new SerializedObject(controller);
            if (serialized.FindProperty("faceDraw").objectReferenceValue == null) throw new Exception("FaceDraw reference missing.");
            var palette = serialized.FindProperty("bodyMaterials");
            if (palette.arraySize != 6) throw new Exception("Palette must match the player's six body materials.");
            for (int i = 0; i < palette.arraySize; ++i)
                if (palette.GetArrayElementAtIndex(i).objectReferenceValue == null) throw new Exception("Missing palette material.");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WorkFolder/PAP/Prefabs/Players/Player (1).prefab");
            if (prefab.GetComponent<PlayerSavedFace>() == null) throw new Exception("PlayerSavedFace missing.");
            instance = UnityEngine.Object.Instantiate(prefab);
            var renderer = instance.GetComponent<Player>().MeshRenderer;
            var source = renderer.sharedMesh;
            if (source == null) throw new Exception("Gameplay face renderer has no mesh.");
            var modelPath = AssetDatabase.GetAssetPath(source);
            var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (!source.isReadable || (importer != null && !importer.isReadable))
                throw new Exception($"Gameplay face model '{modelPath}' (mesh '{source.name}') requires Read/Write Enabled for runtime face UV projection.");
            Debug.Log($"[FacePersistence Validation] Face model: {modelPath}; mesh: {source.name}; readable: {source.isReadable}");
            if (source.subMeshCount < 2 || renderer.sharedMaterials.Length < 2) throw new Exception("Missing face slot.");
            FaceDrawShared.ProjectFaceUV(renderer, 1);
            projected = renderer.sharedMesh;
            if (projected == source || projected.uv.Length != source.vertexCount) throw new Exception("Face UV projection failed.");

            input = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            var pixels = new Color32[512 * 512];
            for (int i = 0; i < pixels.Length; ++i) pixels[i] = new Color32(201, 142, 100, 255);
            pixels[1024] = new Color32(0, 0, 0, 255);
            input.SetPixels32(pixels);
            input.Apply();
            var face = new SavedFace { BodyColor = 3, Png = Convert.ToBase64String(input.EncodeToPNG()) };
            face = JsonUtility.FromJson<SavedFace>(JsonUtility.ToJson(face));
            output = new Texture2D(2, 2);
            if (!output.LoadImage(face.Decode()) || face.BodyColor != 3) throw new Exception("PNG/JSON restore failed.");
            var restored = output.GetPixels32();
            if (!restored[0].Equals(pixels[0]) || !restored[1024].Equals(pixels[1024])) throw new Exception("Skin or line pixels changed.");
            var skin = FaceDrawShared.GetColor((Material)palette.GetArrayElementAtIndex(1).objectReferenceValue);
            input.SetPixel(0, 0, Color.white);
            input.SetPixel(1, 0, Color.black);
            input.Apply();
            FaceDrawShared.ApplySkinBackground(input, Color.white, skin);
            var rebased = input.GetPixels32();
            Color32 expectedSkin = skin;
            expectedSkin.a = 255;
            if (!rebased[0].Equals(expectedSkin) || !rebased[1].Equals((Color32)Color.black))
                throw new Exception("Legacy white background correction changed a line or failed to apply skin.");
            input.SetPixel(2, 0, Color.white); // An intentional white stroke in the new format.
            input.Apply();
            var coloredFace = new SavedFace { BodyColor = 1, BackgroundColor = skin, Png = Convert.ToBase64String(input.EncodeToPNG()) };
            coloredFace = JsonUtility.FromJson<SavedFace>(JsonUtility.ToJson(coloredFace));
            output.LoadImage(coloredFace.Decode());
            FaceDrawShared.ApplySkinBackground(output, coloredFace.StoredBackground, skin);
            var coloredPixels = output.GetPixels32();
            if (!coloredPixels[0].Equals(expectedSkin) || !coloredPixels[1].Equals((Color32)Color.black)
                || !coloredPixels[2].Equals((Color32)Color.white))
                throw new Exception("Background metadata roundtrip changed skin, black or white strokes.");
            Debug.Log("[FacePersistence Validation] PASS: FaceDraw references, six skin materials, MainScene player prefab, face UV projection, skin + line PNG/JSON roundtrip. Cloud/multiplayer not exercised.");
        }
        catch (Exception ex) { Debug.LogException(ex); }
        finally
        {
            if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
            if (projected != null) UnityEngine.Object.DestroyImmediate(projected);
            if (input != null) UnityEngine.Object.DestroyImmediate(input);
            if (output != null) UnityEngine.Object.DestroyImmediate(output);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
