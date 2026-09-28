using System.Collections.Generic;
using UnityEngine;

namespace WorkFolder.PAP.Scripts.MapSystem
{
    // 맵 프리팹의 루트에 붙는다. 이 맵의 스폰 지점을 들고 있고, 비어 있는 지점을 골라 준다.
    // 맵은 네트워크 오브젝트가 아니다. 모든 클라이언트가 같은 프리팹을 각자 로컬로 생성한다.
    [DisallowMultipleComponent]
    public sealed class MapDefinition : MonoBehaviour
    {
        [Header("Spawn")]
        [SerializeField] private Transform[] spawnPoints;
        // 같은 지점에 여러 명이 몰리지 않게 지점 주변으로 흩뿌리는 반경(수평).
        [SerializeField, Min(0f)] private float spawnSpread = 1f;

        public IReadOnlyList<Transform> SpawnPoints => spawnPoints;

        // 다른 플레이어에게서 가장 먼 스폰 지점을 고른다. 지점이 없으면 맵 원점을 쓴다.
        public void GetSpawnPose(
            IEnumerable<Vector3> occupiedPositions,
            out Vector3 position,
            out Quaternion rotation)
        {
            Transform best = null;
            float bestDistance = float.NegativeInfinity;

            if (spawnPoints != null)
            {
                foreach (Transform point in spawnPoints)
                {
                    if (point == null)
                        continue;

                    // 동점이면 매번 같은 곳이 되지 않도록 아주 작은 무작위 값을 더한다.
                    float distance = NearestDistance(point.position, occupiedPositions)
                                   + Random.value * 0.01f;
                    if (distance <= bestDistance)
                        continue;

                    best = point;
                    bestDistance = distance;
                }
            }

            Transform origin = best != null ? best : transform;
            Vector2 spread = Random.insideUnitCircle * spawnSpread;
            position = origin.position + new Vector3(spread.x, 0f, spread.y);

            // 캐릭터는 수평으로만 돌기 때문에 스폰 지점의 좌우 방향만 쓴다.
            Vector3 forward = Vector3.ProjectOnPlane(origin.forward, Vector3.up);
            rotation = forward.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(forward.normalized, Vector3.up)
                : Quaternion.identity;
        }

        private static float NearestDistance(Vector3 point, IEnumerable<Vector3> occupiedPositions)
        {
            float nearest = float.PositiveInfinity;
            if (occupiedPositions == null)
                return nearest;

            foreach (Vector3 occupied in occupiedPositions)
                nearest = Mathf.Min(nearest, Vector3.Distance(point, occupied));

            return nearest;
        }

        // 이름에 "spawn"이 들어간 자식을 찾아 스폰 지점으로 등록한다.
        // 그런 오브젝트가 자식을 갖고 있으면(PlayerSpawnpoints처럼 묶음이면) 그 자식들을 쓴다.
        public int CollectSpawnPoints()
        {
            var found = new List<Transform>();
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child == transform
                    || !child.name.ToLowerInvariant().Contains("spawn")
                    || HasSpawnNamedAncestor(child))
                    continue;

                if (child.childCount == 0)
                {
                    found.Add(child);
                    continue;
                }

                foreach (Transform point in child)
                    found.Add(point);
            }

            spawnPoints = found.ToArray();
            return spawnPoints.Length;
        }

        private bool HasSpawnNamedAncestor(Transform child)
        {
            for (Transform parent = child.parent; parent != null && parent != transform; parent = parent.parent)
            {
                if (parent.name.ToLowerInvariant().Contains("spawn"))
                    return true;
            }

            return false;
        }

        private void OnDrawGizmos()
        {
            if (spawnPoints == null)
                return;

            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.9f);
            foreach (Transform point in spawnPoints)
            {
                if (point == null)
                    continue;

                Gizmos.DrawWireSphere(point.position + Vector3.up * 0.1f, Mathf.Max(0.25f, spawnSpread));
                Gizmos.DrawLine(point.position, point.position + point.forward);
            }
        }
    }
}
