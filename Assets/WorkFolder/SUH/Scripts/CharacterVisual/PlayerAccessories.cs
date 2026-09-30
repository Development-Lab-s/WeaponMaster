using System;
using System.Collections.Generic;
using Unity.Services.CloudCode;
using UnityEngine;
using WorkFolder.PAP.Scripts.Agent.Player;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;

namespace WorkFolder.SUH.Scripts.CharacterVisual
{
    // Installed by Player.Spawned on every peer, including late joiners.
    public sealed class PlayerAccessories : MonoBehaviour
    {
        private Player player;
        private GameObject first, second, policeHat, crown;
        private string verifiedId;
        private bool isAdmin, loading;
        private bool adminStatusKnown;
        private bool initialAdminCheckFinished;
        private HeadwearReveal policeReveal, crownReveal;
        private float refreshAt;

        [Serializable]
        [UnityEngine.Scripting.Preserve]
        private sealed class AdminStatus
        {
            public string playerId;
            public bool isAdmin;
        }

        private void Awake()
        {
            player = GetComponent<Player>();
            first = AccessoryVisuals.Find(transform, "Torus.002");
            second = AccessoryVisuals.Find(transform, "Torus.003");
            policeHat = AccessoryVisuals.Find(transform, "policehat");
            crown = AccessoryVisuals.Find(transform, "Crown");
            policeReveal = new HeadwearReveal(policeHat, transform, new Color(0.45f, 0.75f, 1f));
            crownReveal = new HeadwearReveal(crown, transform, new Color(1f, 0.8f, 0.2f));
            Apply(false);
        }

        private void Apply(bool spawned)
        {
            AccessoryVisuals.Show(first, spawned && player.AccessoryStyleIndex == 1);
            AccessoryVisuals.Show(second, spawned && player.AccessoryStyleIndex == 2);
            // Give the initial lookup a chance, but a missing endpoint must not disable rank rewards.
            bool showAdmin = spawned && adminStatusKnown && isAdmin;
            bool showCrown = spawned && initialAdminCheckFinished && !isAdmin && player.IsLocalLeaderboardLeader;
            policeReveal?.SetVisible(showAdmin);
            crownReveal?.SetVisible(showCrown);
        }

        private void LateUpdate()
        {
            if (player == null || player.Object == null || !player.Object.IsValid) { Apply(false); return; }
            string id = player.playerId.ToString();
            if (id != verifiedId)
            {
                isAdmin = false;
                adminStatusKnown = false;
                initialAdminCheckFinished = false;
                policeReveal?.SetVisible(false);
                crownReveal?.SetVisible(false);
                verifiedId = id;
                refreshAt = 0;
            }
            Apply(true);
            if (!loading && !string.IsNullOrEmpty(id) && Time.unscaledTime >= refreshAt)
                RefreshAdmin(id);
        }

        private async void RefreshAdmin(string id)
        {
            loading = true;
            try
            {
                await UgsSignIn.EnsureSignedInAsync();
                // Each observer checks UGS directly instead of trusting an owner-supplied admin flag.
                var result = await CloudCodeService.Instance.CallEndpointAsync<AdminStatus>(
                    "GetPlayerAdminStatus", new Dictionary<string, object> { { "targetPlayerId", id } });
                if (this == null || player.Object == null || !player.Object.IsValid || player.playerId.ToString() != id) return;
                if (result == null || result.playerId != id) throw new InvalidOperationException("Invalid admin response.");
                isAdmin = result.isAdmin;
                adminStatusKnown = true;
                initialAdminCheckFinished = true;
                refreshAt = Time.unscaledTime + 60f;
            }
            catch (Exception ex)
            {
                if (this == null || player == null || player.Object == null || !player.Object.IsValid || player.playerId.ToString() != id) return;
                // Preserve the last verified status during transient outages; never grant on failure.
                initialAdminCheckFinished = true;
                refreshAt = Time.unscaledTime + 30f;
                Debug.LogWarning($"Admin status unavailable; retaining last verified status and retrying: {ex.Message}", this);
            }
            finally { loading = false; }
        }

        private void OnDisable()
        {
            policeReveal?.SetVisible(false);
            crownReveal?.SetVisible(false);
        }

        private void OnDestroy()
        {
            policeReveal?.Dispose();
            crownReveal?.Dispose();
        }
    }
}
