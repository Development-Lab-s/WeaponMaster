using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Random = UnityEngine.Random;
using DevLib.ServiceLocator;
using DG.Tweening;
using UnityEngine;
using WorkFolder.PAP.Scripts.MapSystem;
using WorkFolder.SUH.Scripts.RoundSystem.Lib.Interface;

namespace WorkFolder.SUH.Scripts.RoundSystem
{
    // Adapter for the existing prefab loader. Scene changes would destroy MainScene's UI/runner.
    public sealed class MapService : MonoBehaviour, IMapService
    {
        [SerializeField] private MapLoader loader;
        [SerializeField] private MapCatalogSO catalog;
        [Tooltip("Random rotation pool. Register these prefabs in the same MapCatalog used by the lobby. Empty = all catalog maps.")]
        [SerializeField] private MapDefinition[] maps;

        private ChessboardTiles chessboard;
        public int ChessTileCount => chessboard?.Count ?? 0;
        public int GetFallableChessTileCount(float protectionRadius)
            => chessboard?.GetFallableCount(protectionRadius) ?? 0;
        public void ApplyChessTiles(int seed, int fallenCount, float elapsedSinceFirstFall, ChessBalance balance)
            => chessboard?.Apply(seed, fallenCount, elapsedSinceFirstFall, balance);

        private void PrepareMap(MapDefinition map)
        {
            chessboard?.Dispose();
            chessboard = ChessboardTiles.Create(map);
        }

        public bool IsLoading { get; private set; }
        public int CurrentMapIndex { get; private set; } = -1;
        public bool HasMap => MapLoader.Current != null;

        private void Awake()
        {
            if (catalog == null) catalog = MapCatalogSO.LoadDefault();
            if (loader == null) loader = FindFirstObjectByType<MapLoader>();
            ServiceLocator.Register<IMapService>(this);
            MapLoader.MapLoaded += PrepareMap;
            if (MapLoader.Current != null) PrepareMap(MapLoader.Current);
        }

        private void OnDestroy()
        {
            chessboard?.Dispose();
            MapLoader.MapLoaded -= PrepareMap;
            ServiceLocator.UnRegister<IMapService>(this);
        }

        public int ChooseNextMap(int previous)
        {
            var candidates = new List<int>();
            if (catalog == null) return -1;
            for (int i = 0; i < catalog.Count; i++)
            {
                if (!catalog.IsValidIndex(i)) continue;
                if (maps != null && maps.Length > 0 && System.Array.IndexOf(maps, catalog.GetPrefab(i)) < 0)
                    continue;
                candidates.Add(i);
            }
            if (candidates.Count > 1) candidates.Remove(previous);
            return candidates.Count == 0 ? -1 : candidates[Random.Range(0, candidates.Count)];
        }

        public bool ApplyMap(int index)
        {
            if (loader == null || catalog == null || !catalog.IsValidIndex(index)) return false;
            IsLoading = true;
            MapDefinition previous = MapLoader.Current;
            // MapLoader exposes no public reload API. Unity's message bridge reuses its existing
            // Load(int), including Current/MapLoaded updates required by the original spawner.
            loader.enabled = false;
            loader.SendMessage("Load", index, SendMessageOptions.RequireReceiver);
            bool loaded = MapLoader.Current != null && MapLoader.Current != previous;
            if (loaded)
            {
                if (previous != null)
                {
                    previous.gameObject.SetActive(false);
                    Destroy(previous.gameObject);
                }
                CurrentMapIndex = index;
                Physics.SyncTransforms();
            }
            IsLoading = false;
            return loaded;
        }

        public void AdoptInitialMap(int index)
        {
            // Leave the room creator's first map intact, including its original spawn points.
            if (HasMap) CurrentMapIndex = index;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            MapCatalogSO source = catalog != null ? catalog : MapCatalogSO.LoadDefault();
            if (source == null || maps == null) return;
            foreach (MapDefinition map in maps)
            {
                if (map == null) continue;
                bool registered = false;
                for (int i = 0; i < source.Count; i++)
                    if (source.GetPrefab(i) == map) registered = true;
                if (!registered)
                    Debug.LogWarning($"[RoundSystem] Register '{map.name}' in the lobby MapCatalog before adding it to the rotation.", this);
            }
        }
#endif
    }

    // Local presentation of replicated seed/count; imported map models need no NetworkObjects.
    internal sealed class ChessboardTiles : IDisposable
    {
        private readonly Transform[] tiles;
        private readonly Vector3[] origins;
        private readonly Collider[][] colliders;
        private readonly Quaternion[] rotations;
        private readonly Sequence[] animations;
        private ChessBalance activeBalance;
        private readonly Bounds[] originalBounds;
        private readonly Vector3[] spawnPositions;
        private bool[] protectedTiles;
        private float cachedProtectionRadius = -1f;
        private int fallableCount;
        private const float WarningDip = 0.12f;
        private const float FallDistance = 60f;
        private int[] order;
        private float[] restoreOffsets;
        private int orderSeed;
        public int Count => tiles.Length;

        public static ChessboardTiles Create(MapDefinition map)
        {
            if (map == null) return null;
            // Model replacements acquire names such as "Chessboard_subtract_object 1".
            // Recognize the tile structure inside the active map, not an imported root filename.
            Transform[] tiles = map.GetComponentsInChildren<Transform>(true)
                .Where(t => Regex.IsMatch(t.name, @"^Tile_R\d+_C\d+_(Black|White)$"))
                .OrderBy(t => t.name, StringComparer.Ordinal).ToArray();
            if (tiles.Length == 0) return null;
            return new ChessboardTiles(map, tiles);
        }

        private ChessboardTiles(MapDefinition map, Transform[] tileTransforms)
        {
            tiles = tileTransforms;
            spawnPositions = map.SpawnPoints == null ? Array.Empty<Vector3>() : map.SpawnPoints
                .Where(point => point != null).Select(point => point.position).ToArray();
            if (spawnPositions.Length == 0) spawnPositions = new[] { map.transform.position };
            originalBounds = new Bounds[tiles.Length];
            origins = tiles.Select(t => t.position).ToArray();
            rotations = tiles.Select(t => t.rotation).ToArray();
            animations = new Sequence[tiles.Length];
            colliders = new Collider[tiles.Length][];
            // Imported GLB tiles have meshes but no physics. Give each mesh its own box.
            for (int i = 0; i < tiles.Length; i++)
            {
                foreach (MeshFilter mesh in tiles[i].GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mesh.sharedMesh == null || mesh.GetComponents<Collider>().Any(c => !c.isTrigger)) continue;
                    BoxCollider box = mesh.gameObject.AddComponent<BoxCollider>();
                    box.center = mesh.sharedMesh.bounds.center;
                    box.size = mesh.sharedMesh.bounds.size;
                }
                colliders[i] = tiles[i].GetComponentsInChildren<Collider>(true);
            }
            Physics.SyncTransforms();
            // Snapshot before any animation; moving/hidden collider bounds must not affect protection.
            for (int i = 0; i < tiles.Length; i++)
            {
                Bounds bounds = new Bounds(origins[i], Vector3.zero);
                foreach (Collider collider in colliders[i])
                    if (collider != null && !collider.isTrigger) bounds.Encapsulate(collider.bounds);
                originalBounds[i] = bounds;
            }
            // Remove only the old broad supporting boxes, preserving perimeter and death triggers.
            foreach (BoxCollider box in map.GetComponentsInChildren<BoxCollider>(true))
            {
                if (box.isTrigger || tiles.Any(t => box.transform.IsChildOf(t))) continue;
                if (!box.GetComponentsInParent<Transform>(true).Any(t =>
                        t.name == "chessboard_collider" ||
                        t.name.StartsWith("Chessboard_subtract_object", StringComparison.Ordinal))) continue;
                Bounds bounds = box.bounds;
                int supportedTiles = origins.Count(p => p.x > bounds.min.x && p.x < bounds.max.x &&
                                                       p.z > bounds.min.z && p.z < bounds.max.z);
                if (supportedTiles > 1) box.enabled = false;
            }
            Physics.SyncTransforms();
        }

        public int GetFallableCount(float radius)
        {
            radius = Mathf.Max(0f, radius);
            if (protectedTiles != null && cachedProtectionRadius == radius) return fallableCount;
            cachedProtectionRadius = radius;
            protectedTiles = new bool[Count];
            fallableCount = 0;
            for (int i = 0; i < Count; i++)
            {
                Bounds bounds = originalBounds[i];
                foreach (Vector3 spawn in spawnPositions)
                {
                    // Horizontal distance to the tile footprint, not its center or spawn height.
                    float dx = Mathf.Max(bounds.min.x - spawn.x, 0f, spawn.x - bounds.max.x);
                    float dz = Mathf.Max(bounds.min.z - spawn.z, 0f, spawn.z - bounds.max.z);
                    if (dx * dx + dz * dz <= radius * radius + 0.0001f)
                    {
                        protectedTiles[i] = true;
                        break;
                    }
                }
                if (!protectedTiles[i]) fallableCount++;
            }
            order = null;
            return fallableCount;
        }

        private Sequence CreateAnimation(int index, float restoreAge)
        {
            Transform tile = tiles[index];
            Vector3 origin = origins[index];
            Quaternion rotation = rotations[index];
            Sequence sequence = DOTween.Sequence().SetAutoKill(false).Pause();
            // Tween 1: hesitate, wobble, and settle slightly. Deterministic, unlike random shake.
            sequence.Append(DOVirtual.Float(0f, 1f, activeBalance.WarningSeconds, progress =>
            {
                if (tile == null) return;
                float wobble = Mathf.Sin(progress * Mathf.PI * 8f) * Mathf.Sin(progress * Mathf.PI);
                tile.position = origin + new Vector3(wobble * 0.06f, -WarningDip * progress + wobble * 0.035f, 0f);
                tile.rotation = rotation * Quaternion.Euler(wobble * 2.5f, 0f, wobble * 1.5f);
            }).SetEase(Ease.Linear));
            // Tween 2: accelerate downwards only after the warning finishes.
            sequence.Append(DOVirtual.Float(0f, 1f, activeBalance.FallSeconds, progress =>
            {
                if (tile == null) return;
                tile.position = origin + Vector3.down * (WarningDip + FallDistance * progress);
                tile.rotation = rotation * Quaternion.Euler(progress * 12f, 0f, progress * 6f);
            }).SetEase(Ease.InCubic));
            sequence.AppendInterval(Mathf.Max(0f, restoreAge - activeBalance.WarningSeconds - activeBalance.FallSeconds));
            sequence.Append(DOVirtual.Float(0f, 1f, activeBalance.RestoreRiseSeconds, progress =>
            {
                if (tile == null) return;
                tile.SetPositionAndRotation(origin + Vector3.down * (3f * (1f - progress)), rotation);
            }).SetEase(Ease.OutCubic));
            return sequence;
        }

        public void Dispose()
        {
            for (int i = 0; i < animations.Length; i++)
            {
                animations[i]?.Kill();
                animations[i] = null;
            }
        }

        public void Apply(int seed, int fallenCount, float elapsedSinceFirstFall, ChessBalance balance)
        {
            if (activeBalance.WarningSeconds != balance.WarningSeconds || activeBalance.FallSeconds != balance.FallSeconds ||
                activeBalance.RestoreSeconds != balance.RestoreSeconds ||
                activeBalance.RestoreSpreadSeconds != balance.RestoreSpreadSeconds ||
                activeBalance.RestoreRiseSeconds != balance.RestoreRiseSeconds ||
                activeBalance.FirstWarningSeconds != balance.FirstWarningSeconds ||
                activeBalance.BatchIntervalSeconds != balance.BatchIntervalSeconds ||
                activeBalance.TilesPerBatch != balance.TilesPerBatch)
                Dispose();
            activeBalance = balance;
            GetFallableCount(balance.SpawnProtectionRadius);
            if (order == null || orderSeed != seed)
            {
                Dispose();
                orderSeed = seed;
                // Keep protected tiles after the selectable prefix, permanently on the board.
                order = Enumerable.Range(0, Count).Where(i => !protectedTiles[i])
                    .Concat(Enumerable.Range(0, Count).Where(i => protectedTiles[i])).ToArray();
                // Explicit integer PRNG: identical shuffle on Mono/IL2CPP and every client.
                uint random = unchecked((uint)seed) | 1u;
                for (int i = fallableCount - 1; i > 0; i--)
                {
                    random ^= random << 13;
                    random ^= random >> 17;
                    random ^= random << 5;
                    int j = (int)(random % (uint)(i + 1));
                    int swap = order[i]; order[i] = order[j]; order[j] = swap;
                }
                // Independent random restoration times: neither fall order nor equal spacing.
                // Derived once from the replicated seed, identical for every peer/late joiner.
                restoreOffsets = new float[Count];
                for (int i = 0; i < Count; i++)
                {
                    random ^= random << 13;
                    random ^= random >> 17;
                    random ^= random << 5;
                    restoreOffsets[i] = (random & 0x00FFFFFFu) / 16777216f;
                }
            }
            int count = Mathf.Clamp(fallenCount, 0, fallableCount);
            for (int i = 0; i < Count; i++)
            {
                int index = order[i];
                Transform tile = tiles[index];
                if (tile == null) continue;
                bool fallen = i < count;
                // Every batch has its own age, so rapid consecutive falls animate together.
                float age = Mathf.Max(0f, elapsedSinceFirstFall -
                    (i / activeBalance.TilesPerBatch) * activeBalance.BatchIntervalSeconds);
                float restoreAge = Mathf.Max(activeBalance.WarningSeconds + activeBalance.FallSeconds + 0.1f,
                    activeBalance.RestoreSeconds - activeBalance.FirstWarningSeconds +
                    restoreOffsets[index] * activeBalance.RestoreSpreadSeconds -
                    (i / activeBalance.TilesPerBatch) * activeBalance.BatchIntervalSeconds);
                bool restoring = fallen && age >= restoreAge;
                bool restored = restoring && age >= restoreAge + activeBalance.RestoreRiseSeconds;
                bool visible = !fallen || age < activeBalance.WarningSeconds + activeBalance.FallSeconds || restoring;
                if (!fallen || restored)
                {
                    animations[index]?.Kill();
                    animations[index] = null;
                    tile.SetPositionAndRotation(origins[index], rotations[index]);
                }
                else if (visible)
                {
                    if (animations[index] == null) animations[index] = CreateAnimation(index, restoreAge);
                    // Seek a paused sequence from the shared clock, including for late joiners.
                    animations[index].Goto(age, false);
                }
                else
                {
                    animations[index]?.Kill();
                    animations[index] = null;
                    tile.position = origins[index] + Vector3.down * (WarningDip + FallDistance);
                }
                foreach (Collider collider in colliders[index])
                    if (collider != null) collider.enabled = !fallen || restored || age < activeBalance.WarningSeconds;
                if (tile.gameObject.activeSelf != visible) tile.gameObject.SetActive(visible);
            }
            Physics.SyncTransforms();
        }
    }

}
