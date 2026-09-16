using System;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;

public sealed class ProximityVoicePlayer : NetworkBehaviour
{
    public override void Spawned()
    {
        if (!HasStateAuthority)
            return;

        _ = BindVoiceAsync();
    }

    private async Task BindVoiceAsync()
    {
        try
        {
            if (VivoxManager.Instance == null)
            {
                Debug.LogError("VivoxManager not found.");
                return;
            }

            string roomName = Runner.SessionInfo.Name;

            await VivoxManager.Instance.BindToFusionRoomAsync(
                roomName,
                gameObject
            );
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    public override void Despawned(
        NetworkRunner runner,
        bool hasState)
    {
        if (!hasState)
            return;

        VivoxManager.Instance?.UnbindLocalPlayer(gameObject);
    }
}