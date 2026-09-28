using System;
using System.Collections.Generic;
using DevLib.ServiceLocator;
using Fusion;
using UnityEngine;
using WorkFolder.PAP.Scripts.Agent.Player;
using WorkFolder.PAP.Scripts.MapSystem;
using WorkFolder.PAP.Scripts.Network;
using WorkFolder.PAP.Scripts.SO.DataFolder;
using WorkFolder.PAP.Scripts.WeaponSystem;
using Random = UnityEngine.Random;

namespace WorkFolder.PAP.Scripts.SelectUIScene
{
    public class PlayerSpawner : SimulationBehaviour
    {
        [SerializeField] private EquipSocketContainer container;
        [SerializeField] private NetworkObject playerPrefab;
        [SerializeField] private PartOffsetDataSO[] partDataList;
        [SerializeField] private Transform[] playerSpawnPoints;
            
        private Action end;
        
        Transform FindChildHelper(Transform current, string targetName)
        {
            if (current.name == targetName) return current;

            foreach (Transform child in current)
            {
                Transform found = FindChildHelper(child, targetName);
                if (found != null) return found;
            }

            return null;
        }

        private void Awake()
        {
            end += HandleSpawnCharacter;
        }

        public void SpawnCharacter()
        {
            Trans.Instance.Play(end);
        }

        private void HandleSpawnCharacter()
        {
            NetworkRunner runner = null;

            foreach (NetworkRunner candidate in
                     FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None))
            {
                if (candidate != null && candidate.IsRunning)
                {
                    runner = candidate;
                    break;
                }
            }

            if (runner == null) return;
            (int, ItemData) pack = container.GetCurrentPartAndWeapon();
            GetSpawnPose(out Vector3 spawnPosition, out Quaternion spawnRotation);
            NetworkObject player = runner.Spawn(playerPrefab, spawnPosition, spawnRotation, inputAuthority:runner.LocalPlayer);
            runner.SetPlayerObject(runner.LocalPlayer, player);
            Player playerHandler = player.GetComponent<Player>();
            PartOffsetDataSO partData = partDataList[pack.Item1];
            playerHandler.SetInitialize(partData,FindChildHelper(player.transform,partData.targetPartName).transform.GetComponent<NetworkObject>(),pack.Item2.ItemPrefab);
            Trans.Instance.DePlay();
        }

        // 방을 만들 때 고른 맵이 생성돼 있으면 그 맵의 스폰 지점 중 다른 플레이어에게서
        // 가장 먼 곳을 쓴다. 맵이 없으면(맵 시스템을 쓰지 않는 씬) 기존 방식 그대로 스폰한다.
        private void GetSpawnPose(out Vector3 position, out Quaternion rotation)
        {
            if (MapLoader.Current != null)
            {
                MapLoader.Current.GetSpawnPose(OtherPlayerPositions(), out position, out rotation);
                return;
            }

            rotation = Quaternion.identity;
            var points = new List<Transform>();
            if (playerSpawnPoints != null)
            {
                foreach (Transform point in playerSpawnPoints)
                {
                    if (point != null)
                        points.Add(point);
                }
            }

            Vector3 origin = points.Count > 0
                ? points[Random.Range(0, points.Count)].position
                : transform.position;
            position = origin + new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));
        }

        private static IEnumerable<Vector3> OtherPlayerPositions()
        {
            if (Player.All == null)
                yield break;

            foreach (Player other in Player.All)
            {
                if (other != null)
                    yield return other.transform.position;
            }
        }
    }
}