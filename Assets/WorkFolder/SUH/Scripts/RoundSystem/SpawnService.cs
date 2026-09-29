using System;
using System.Linq;
using System.Collections.Generic;
using DevLib.ServiceLocator;
using Fusion;
using UnityEngine;
using WorkFolder.PAP.Scripts.Agent.Player;
using WorkFolder.PAP.Scripts.MapSystem;
using WorkFolder.PAP.Scripts.SO.DataFolder;
using WorkFolder.SUH.Scripts.RoundSystem.Lib.Interface;

namespace WorkFolder.SUH.Scripts.RoundSystem
{
    public sealed class SpawnService : MonoBehaviour, ISpawnService
    {
        [SerializeField] private EquipSocketContainer container;
        [SerializeField] private NetworkObject playerPrefab;
        [SerializeField] private PartOffsetDataSO[] partDataList;
        [SerializeField] private PlayerInputSO playerInput;
        [SerializeField] private DropText deathUI;
        public bool HasParticipated { get; private set; }
        private NetworkRunner runner;
        private int spawnedRound = -1;
        private readonly List<Player> suspendedPlayers = new List<Player>();

        public void Bind(NetworkRunner value) => runner = value;
        private void Awake() => ServiceLocator.Register<ISpawnService>(this);
        private void OnDestroy() => ServiceLocator.UnRegister<ISpawnService>(this);

        public void ObservePlayers()
        {
            if (runner == null || Player.All == null) return;
            if (Player.All.Any(p => p != null && p.Object != null && p.Object.IsValid &&
                                    p.Runner == runner && p.Object.InputAuthority == runner.LocalPlayer))
                HasParticipated = true;
        }

        public void SuspendPlayers()
        {
            if (runner == null) return;
            ObservePlayers();
            if (deathUI != null)
            {
                deathUI.StopAllCoroutines();
                deathUI.DisableUis();
            }
            if (Player.All == null) return;
            foreach (Player player in Player.All.ToArray())
            {
                if (player == null || player.Object == null || !player.Object.IsValid || player.Runner != runner)
                    continue;
                suspendedPlayers.Add(player);
                RemoveInputCallback(player);
                // Keep network state alive during the result screen so pending kill-save tasks can finish.
                // Inactive roots remove renderers, colliders and ragdoll physics from the arena immediately.
                player.gameObject.SetActive(false);
            }
        }

        private void RemoveInputCallback(Player player)
        {
            if (playerInput == null || playerInput.OnAttackKeyPressed == null) return;
            foreach (Delegate callback in playerInput.OnAttackKeyPressed.GetInvocationList())
                if (ReferenceEquals(callback.Target, player))
                    playerInput.OnAttackKeyPressed -= (Action<bool>)callback;
        }

        public void ClearPlayers()
        {
            if (runner == null) return;
            ObservePlayers();
            if (deathUI != null)
            {
                deathUI.StopAllCoroutines();
                deathUI.DisableUis();
            }
            SuspendPlayers();
            foreach (Player player in suspendedPlayers.ToArray())
            {
                if (player == null || player.Object == null || !player.Object.IsValid || !player.HasStateAuthority)
                    continue;
                RemoveInputCallback(player);

                // Weapons are separately spawned root NetworkObjects, although parented under the player.
                foreach (NetworkObject child in player.GetComponentsInChildren<NetworkObject>(true).Reverse().ToArray())
                    if (child != player.Object && child.IsValid && !child.IsNested && child.HasStateAuthority)
                        runner.Despawn(child);
                runner.Despawn(player.Object);
            }
            suspendedPlayers.RemoveAll(p => p == null || p.Object == null || !p.Object.IsValid);
        }

        public void SpawnPlayersAtRandomPoints(int roundNumber)
        {
            if (!HasParticipated || spawnedRound == roundNumber || runner == null || !runner.IsRunning ||
                MapLoader.Current == null || playerPrefab == null || container == null) return;
            if (runner.TryGetPlayerObject(runner.LocalPlayer, out NetworkObject existing) && existing != null && existing.IsValid)
            {
                spawnedRound = roundNumber;
                return;
            }
            // Same equipment initialization as PAP/SelectUIScene/PlayerSpawner, without its menu transition.
            (int partIndex, ItemData weapon) = container.GetCurrentPartAndWeapon();
            if (weapon == null || partDataList == null || partIndex < 0 || partIndex >= partDataList.Length ||
                partDataList[partIndex] == null) return;
            PartOffsetDataSO part = partDataList[partIndex];
            var positions = Player.All == null ? Array.Empty<Vector3>() : Player.All
                .Where(p => p != null && p.Object != null && p.Object.IsValid && p.Runner == runner)
                .Select(p => p.transform.position).ToArray();
            MapLoader.Current.GetSpawnPose(positions, out Vector3 position, out Quaternion rotation);
            NetworkObject obj = runner.Spawn(playerPrefab, position, rotation, inputAuthority: runner.LocalPlayer);
            Player player = obj.GetComponent<Player>();
            Transform target = obj.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == part.targetPartName);
            if (target == null || !target.TryGetComponent(out NetworkObject targetObject))
            {
                Debug.LogError("[RoundSystem] Selected body part has no NetworkObject.", this);
                runner.Despawn(obj);
                return;
            }
            runner.SetPlayerObject(runner.LocalPlayer, obj);
            player.SetInitialize(part, targetObject, weapon.ItemPrefab);
            player.TeleportTo(position, rotation);
            spawnedRound = roundNumber;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
