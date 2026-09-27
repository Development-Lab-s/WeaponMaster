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

        // 새 Local Player가 Spawn됐다는 것은
        // 플레이 가능한 상태라고 본다.
        VivoxManager.Instance?.SetPlayerAlive(true);

        _ = BindVoiceAsync();
    }


    private async Task BindVoiceAsync()
    {
        try
        {
            if (VivoxManager.Instance == null)
            {
                Debug.LogError(
                    "VivoxManager not found."
                );

                return;
            }

            string roomName =
                Runner.SessionInfo.Name;

            await VivoxManager.Instance
                .BindToFusionRoomAsync(
                    roomName,
                    gameObject
                );
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }


    // 죽음 시스템에서 호출.
    public void NotifyDeath()
    {
        if (!HasStateAuthority)
            return;

        VivoxManager.Instance?
            .SetPlayerAlive(false);
    }


    // 같은 NetworkObject를 재사용하는 리스폰 구조라면 호출.
    public void NotifyRespawn()
    {
        if (!HasStateAuthority)
            return;

        VivoxManager.Instance?
            .SetPlayerAlive(true);
    }


    public override void Despawned(
        NetworkRunner runner,
        bool hasState)
    {
        if (!hasState)
            return;

        VivoxManager manager =
            VivoxManager.Instance;

        if (manager == null)
            return;

        // Local Player 자체가 사라졌으면
        // 마이크도 안전하게 차단.
        manager.SetPlayerAlive(false);

        manager.UnbindLocalPlayer(gameObject);
    }
}