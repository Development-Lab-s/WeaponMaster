using System;
using Fusion;
using UnityEngine;
using WorkFolder.PAP.Scripts.Network;

namespace WorkFolder.PAP.Scripts.MapSystem
{
    // MainScene에 하나 둔다. 방에 들어오면 방을 만든 사람이 고른 맵(세션 속성 "map")을 읽어
    // 그 맵 프리팹을 로컬로 한 번 생성한다. 나중에 들어온 사람도 세션 정보로 같은 번호를 받는다.
    public sealed class MapLoader : MonoBehaviour
    {
        public static MapDefinition Current { get; private set; }
        public static event Action<MapDefinition> MapLoaded;

        [Tooltip("비워 두면 Resources/MapCatalog를 쓴다.")]
        [SerializeField] private MapCatalogSO catalog;

        [Tooltip("세션에 맵 번호가 없을 때(MainScene에서 바로 실행 등) 쓸 맵.")]
        [SerializeField, Min(0)] private int fallbackMapIndex;

        [Tooltip("생성한 맵을 넣을 부모. 비워 두면 씬 최상위.")]
        [SerializeField] private Transform mapParent;

        private MapDefinition _spawnedMap;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = null;
            MapLoaded = null;
        }

        private void Awake()
        {
            if (catalog == null)
                catalog = MapCatalogSO.LoadDefault();
        }

        // 러너가 세션에 들어가는 시점은 씬 로드와 별개라 준비될 때까지 기다렸다가 한 번만 생성한다.
        private void Update()
        {
            NetworkRunner runner = FindRunningRunner();
            if (runner == null || !runner.SessionInfo.IsValid)
                return;

            Load(ReadMapIndex(runner.SessionInfo));
            enabled = false;
        }

        private void OnDestroy()
        {
            if (Current == _spawnedMap)
                Current = null;
        }

        private int ReadMapIndex(SessionInfo session)
        {
            if (session.Properties != null
                && session.Properties.TryGetValue(MapCatalogSO.SessionPropertyKey, out SessionProperty property)
                && property.PropertyValue is int index)
                return index;

            return fallbackMapIndex;
        }

        private void Load(int index)
        {
            if (catalog == null || catalog.Count == 0)
            {
                Debug.LogWarning("[MapLoader] 맵 카탈로그가 비어 있어 맵을 생성하지 않습니다.", this);
                return;
            }

            if (!catalog.IsValidIndex(index))
            {
                Debug.LogWarning($"[MapLoader] 맵 번호 {index}가 카탈로그에 없어 {fallbackMapIndex}번을 씁니다.", this);
                index = fallbackMapIndex;
                if (!catalog.IsValidIndex(index))
                    return;
            }

            MapDefinition prefab = catalog.GetPrefab(index);
            _spawnedMap = Instantiate(prefab, prefab.transform.position, prefab.transform.rotation, mapParent);
            _spawnedMap.name = prefab.name;

            // 같은 프레임에 스폰이 일어나도 새 맵의 콜라이더가 바로 잡히게 한다.
            Physics.SyncTransforms();

            Current = _spawnedMap;
            Debug.Log($"[MapLoader] 맵 생성: {catalog.GetDisplayName(index)} ({index})", this);
            MapLoaded?.Invoke(_spawnedMap);
        }

        private static NetworkRunner FindRunningRunner()
        {
            NetworkRunner current = RunnerProvider.Current;
            if (current != null && current.IsRunning)
                return current;

            foreach (NetworkRunner runner in NetworkRunner.Instances)
            {
                if (runner != null && runner.IsRunning)
                    return runner;
            }

            return null;
        }
    }
}
