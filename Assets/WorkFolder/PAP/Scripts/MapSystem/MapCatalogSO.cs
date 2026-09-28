using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorkFolder.PAP.Scripts.MapSystem
{
    // 방을 만들 때 고를 수 있는 맵 목록. 로비(드롭다운)와 MainScene(MapLoader)이 같은 목록을
    // 봐야 번호가 같은 맵을 가리키므로 Resources/MapCatalog 하나를 공용으로 쓴다.
    [CreateAssetMenu(fileName = "MapCatalog", menuName = "SO/Map/Map Catalog", order = 0)]
    public sealed class MapCatalogSO : ScriptableObject
    {
        public const string ResourcePath = "MapCatalog";

        // 방을 만들 때 고른 맵 번호를 담는 Photon 세션 속성 키.
        public const string SessionPropertyKey = "map";

        [Serializable]
        public struct Entry
        {
            public string displayName;
            public MapDefinition prefab;
        }

        [SerializeField] private List<Entry> maps = new List<Entry>();

        public int Count => maps.Count;

        public static MapCatalogSO LoadDefault()
        {
            return Resources.Load<MapCatalogSO>(ResourcePath);
        }

        public bool IsValidIndex(int index)
        {
            return index >= 0 && index < maps.Count && maps[index].prefab != null;
        }

        public MapDefinition GetPrefab(int index)
        {
            return IsValidIndex(index) ? maps[index].prefab : null;
        }

        public string GetDisplayName(int index)
        {
            if (index < 0 || index >= maps.Count)
                return string.Empty;

            Entry entry = maps[index];
            if (!string.IsNullOrWhiteSpace(entry.displayName))
                return entry.displayName;

            return entry.prefab != null ? entry.prefab.name : $"Map {index}";
        }

#if UNITY_EDITOR
        // 맵 프리팹 만들기 도구가 쓴다. 같은 프리팹이 이미 있으면 이름만 갱신한다.
        public void EditorAddOrUpdate(string displayName, MapDefinition prefab)
        {
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i].prefab != prefab)
                    continue;

                maps[i] = new Entry { displayName = displayName, prefab = prefab };
                return;
            }

            maps.Add(new Entry { displayName = displayName, prefab = prefab });
        }
#endif
    }
}
