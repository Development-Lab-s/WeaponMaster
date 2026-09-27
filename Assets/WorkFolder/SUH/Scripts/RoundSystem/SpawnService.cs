using System.Collections.Generic;
using System.Linq;
using DevLib.ServiceLocator;
using UnityEngine;
using WorkFolder.PAP.Scripts.Agent.Player;
using WorkFolder.SUH.Scripts.RoundSystem.Lib.Interface;

namespace WorkFolder.SUH.Scripts.RoundSystem
{
    public class SpawnService : MonoBehaviour, ISpawnService
    {
        [SerializeField] private Transform[] spawnPoints;

        private void Awake()
        {
            ServiceLocator.Register<ISpawnService>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.UnRegister<ISpawnService>(this);
        }

        public void SpawnPlayersAtRandomPoints(int seed)
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("[SpawnService] 스폰 포인트가 설정되지 않았습니다.");
                return;
            }

            List<Player> players = Player.All?
                .Where(player => player != null)
                .ToList();

            List<Transform> shuffledPoints = spawnPoints
                .Where(point => point != null)
                .ToList();

            if (shuffledPoints.Count == 0)
                return;

            var random = new System.Random(seed);
            shuffledPoints = shuffledPoints.OrderBy(_ => random.Next()).ToList();

            for (int index = 0; index < players.Count; index++)
            {
                Player player = players[index];
                Transform point = shuffledPoints[index % shuffledPoints.Count];

                if (player.HasStateAuthority)
                    player.TeleportTo(point.position, point.rotation);
            }
        }
    }
}
