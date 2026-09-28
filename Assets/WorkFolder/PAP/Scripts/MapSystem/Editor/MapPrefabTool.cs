using System.IO;
using System.Linq;
using Fusion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace WorkFolder.PAP.Scripts.MapSystem.Editor
{
    // 씬에 배치된 맵 오브젝트를 골라 맵 프리팹으로 만들고 카탈로그에 등록한다.
    // 스폰 지점은 이름에 "spawn"이 들어간 자식에서 자동으로 채운다.
    public static class MapPrefabTool
    {
        private const string DefaultPrefabFolder = "Assets/WorkFolder/PAP/Prefabs/Maps";
        private const string CatalogFolder = "Assets/WorkFolder/PAP/Resources";
        private const string CatalogPath = CatalogFolder + "/" + MapCatalogSO.ResourcePath + ".asset";

        [MenuItem("Tools/Map/선택한 오브젝트로 맵 프리팹 만들기")]
        private static void CreateMapPrefabFromSelection()
        {
            Transform[] selection = Selection.transforms
                .Where(t => t != null && !EditorUtility.IsPersistent(t.gameObject))
                .ToArray();

            if (selection.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "맵 프리팹 만들기",
                    "하이어라키에서 맵에 들어갈 오브젝트를 선택하세요.\n(예: Map, Grid, StairCollide들, DeadZone, PlayerSpawnpoints)",
                    "확인");
                return;
            }

            // 맵은 모든 클라이언트가 로컬로 생성한다. NetworkObject가 들어 있으면 Fusion 오브젝트가
            // 클라이언트마다 따로 생겨 동기화가 깨지므로 막는다.
            Transform networked = selection
                .SelectMany(t => t.GetComponentsInChildren<NetworkObject>(true))
                .Select(n => n.transform)
                .FirstOrDefault();
            if (networked != null)
            {
                EditorUtility.DisplayDialog(
                    "맵 프리팹 만들기",
                    $"'{networked.name}'에 NetworkObject가 있습니다.\n맵은 로컬로 생성되므로 NetworkObject를 뺀 뒤 다시 시도하세요.",
                    "확인");
                return;
            }

            Directory.CreateDirectory(DefaultPrefabFolder);
            string path = EditorUtility.SaveFilePanelInProject(
                "맵 프리팹 저장",
                "Map_" + selection[0].gameObject.scene.name,
                "prefab",
                "맵 프리팹을 저장할 위치를 고르세요.",
                DefaultPrefabFolder);
            if (string.IsNullOrEmpty(path))
                return;

            string mapName = Path.GetFileNameWithoutExtension(path);
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            var root = new GameObject(mapName);
            Undo.RegisterCreatedObjectUndo(root, "맵 루트 생성");
            UnitySceneManager.MoveGameObjectToScene(root, selection[0].gameObject.scene);

            foreach (Transform part in selection)
                Undo.SetTransformParent(part, root.transform, "맵 오브젝트 묶기");

            MapDefinition definition = Undo.AddComponent<MapDefinition>(root);
            int spawnCount = definition.CollectSpawnPoints();

            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAssetAndConnect(
                root,
                path,
                InteractionMode.UserAction);
            if (prefabAsset == null)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                EditorUtility.DisplayDialog("맵 프리팹 만들기", "프리팹 저장에 실패했습니다.", "확인");
                return;
            }

            MapCatalogSO catalog = LoadOrCreateCatalog();
            Undo.RecordObject(catalog, "맵 카탈로그 등록");
            catalog.EditorAddOrUpdate(mapName, prefabAsset.GetComponent<MapDefinition>());
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            UnityEngine.SceneManagement.Scene mapScene = root.scene;
            EnsureMapLoaderInScene(mapScene);

            // 게임 시작 시 MapLoader가 이 프리팹을 생성하므로, 씬에 남겨 두면 맵이 두 개가 된다.
            bool removeFromScene = EditorUtility.DisplayDialog(
                "맵 프리팹 만들기",
                $"'{mapName}' 프리팹을 만들고 카탈로그에 등록했습니다. (스폰 지점 {spawnCount}개)\n\n" +
                "씬에 있는 원본을 지울까요?\n게임을 시작하면 MapLoader가 이 맵을 생성하므로 남겨 두면 맵이 두 번 생깁니다.\n" +
                "(다른 스크립트가 이 오브젝트들을 참조하고 있었다면 그 참조는 비게 됩니다. Ctrl+Z로 되돌릴 수 있습니다.)",
                "씬에서 지우기",
                "남겨 두기");
            if (removeFromScene)
                Undo.DestroyObjectImmediate(root);

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(mapScene);

            if (spawnCount == 0)
                Debug.LogWarning($"[MapPrefabTool] '{mapName}'에서 스폰 지점을 찾지 못했습니다. 프리팹을 열어 MapDefinition의 Spawn Points를 채우세요.", prefabAsset);

            Selection.activeObject = prefabAsset;
        }

        [MenuItem("Tools/Map/맵 카탈로그 열기")]
        private static void SelectCatalog()
        {
            Selection.activeObject = LoadOrCreateCatalog();
        }

        private static MapCatalogSO LoadOrCreateCatalog()
        {
            MapCatalogSO catalog = AssetDatabase.LoadAssetAtPath<MapCatalogSO>(CatalogPath);
            if (catalog != null)
                return catalog;

            Directory.CreateDirectory(CatalogFolder);
            catalog = ScriptableObject.CreateInstance<MapCatalogSO>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static void EnsureMapLoaderInScene(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.GetComponentInChildren<MapLoader>(true) != null)
                    return;
            }

            var loaderObject = new GameObject(nameof(MapLoader));
            Undo.RegisterCreatedObjectUndo(loaderObject, "MapLoader 생성");
            UnitySceneManager.MoveGameObjectToScene(loaderObject, scene);
            Undo.AddComponent<MapLoader>(loaderObject);
        }
    }
}
