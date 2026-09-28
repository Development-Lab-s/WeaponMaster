using System;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;

public sealed class ProximityVoicePlayer : NetworkBehaviour
{
    [Header("Voice UI")]
    [SerializeField]
    private GameObject microphoneIcon;


    [Networked, OnChangedRender(nameof(OnSpeakingChanged))]
    public NetworkBool IsSpeaking { get; set; }


    public override void Spawned()
    {
        // OnChangedRender는 최초 Spawn 때 호출되지 않으므로
        // 최초 상태를 직접 반영한다.
        ApplySpeakingVisual();

        if (!HasStateAuthority)
            return;

        VivoxManager manager =
            VivoxManager.Instance;

        if (manager == null)
        {
            Debug.LogError(
                "ProximityVoicePlayer: VivoxManager not found."
            );

            return;
        }

        manager.LocalSpeechDetectedChanged
            += OnLocalSpeechDetectedChanged;

        // 현재 상태를 한 번 직접 반영.
        SetSpeaking(manager.IsLocalSpeaking);

        // 새 Local Player는 살아있는 상태로 시작.
        manager.SetPlayerAlive(true);

        _ = BindVoiceAsync();
    }


    private async Task BindVoiceAsync()
    {
        try
        {
            VivoxManager manager =
                VivoxManager.Instance;

            if (manager == null)
                return;

            string roomName =
                Runner.SessionInfo.Name;

            await manager.BindToFusionRoomAsync(
                roomName,
                gameObject
            );
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }


    // -----------------------------
    // Speaking
    // -----------------------------

    private void OnLocalSpeechDetectedChanged(
        bool isSpeaking)
    {
        if (!HasStateAuthority)
            return;

        SetSpeaking(isSpeaking);
    }


    private void SetSpeaking(bool isSpeaking)
    {
        if (!HasStateAuthority)
            return;

        if (IsSpeaking == isSpeaking)
            return;

        IsSpeaking = isSpeaking;

        ApplySpeakingVisual();
    }


    private void OnSpeakingChanged()
    {
        ApplySpeakingVisual();
    }


    private void ApplySpeakingVisual()
    {
        if (microphoneIcon == null)
            return;

        microphoneIcon.SetActive(IsSpeaking);
    }


    // -----------------------------
    // Death / Respawn
    // -----------------------------

    public void NotifyDeath()
    {
        if (!HasStateAuthority)
            return;

        SetSpeaking(false);

        VivoxManager.Instance?
            .SetPlayerAlive(false);
    }


    public void NotifyRespawn()
    {
        if (!HasStateAuthority)
            return;

        VivoxManager.Instance?
            .SetPlayerAlive(true);
    }


    // -----------------------------
    // Despawn
    // -----------------------------

    public override void Despawned(
        NetworkRunner runner,
        bool hasState)
    {
        if (microphoneIcon != null)
        {
            microphoneIcon.SetActive(false);
        }

        if (!hasState)
            return;

        VivoxManager manager =
            VivoxManager.Instance;

        if (manager == null)
            return;

        manager.LocalSpeechDetectedChanged
            -= OnLocalSpeechDetectedChanged;

        manager.SetPlayerAlive(false);

        manager.UnbindLocalPlayer(
            gameObject
        );
    }
}