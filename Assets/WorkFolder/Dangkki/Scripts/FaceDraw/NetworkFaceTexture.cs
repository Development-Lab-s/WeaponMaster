using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

// #### 네트워크 하는법 몰라서 순수 깡통한테 물어봄 참고하셈 ####
/// <summary>
/// 플레이어 프리팹에 붙여서, FaceDraw 씬에서 확정한 얼굴 PNG를 다른 플레이어에게 보여준다.
/// - 내 플레이어(StateAuthority)가 스폰되면 FaceDrawShared.Png를 조각내서 RPC로 전송.
/// - 나중에 들어온 사람은 그 플레이어가 스폰될 때 요청 RPC를 보내서 따로 받는다.
/// RPC 한 번에 보낼 수 있는 크기가 제한돼 있어서 ChunkSize 단위로 나눠 보낸다.
/// </summary>
public class NetworkFaceTexture : NetworkBehaviour
{
    private const int ChunkSize = 400;
    private const int MaxPngSize = 256 * 1024;

    [Tooltip("비우면 자식의 첫 SkinnedMeshRenderer")]
    [SerializeField] private SkinnedMeshRenderer targetRenderer;
    [Tooltip("얼굴 머티리얼(서브메시) 번호. FaceDraw와 같아야 한다")]
    [SerializeField] private int faceMaterialIndex = 1;

    private byte[] currentPng;
    private int sendId;
    private int lastAppliedId = -1;
    private readonly Dictionary<int, byte[][]> incoming = new Dictionary<int, byte[][]>();

    private Material faceMaterial;
    private Texture2D faceTexture;

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            FaceDrawShared.OnFaceChanged += OnLocalFaceChanged;
            if (FaceDrawShared.HasFace)
                OnLocalFaceChanged(FaceDrawShared.Png);
        }
        else
        {
            // 이미 방에 있던 플레이어의 얼굴은 스폰 시점에 직접 요청해서 받는다
            RPC_RequestFace();
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        FaceDrawShared.OnFaceChanged -= OnLocalFaceChanged;
        if (faceTexture != null)
            Destroy(faceTexture);
    }

    private void OnLocalFaceChanged(byte[] png)
    {
        if (png == null || png.Length == 0)
            return;

        if (png.Length > MaxPngSize)
        {
            Debug.LogWarning($"[NetworkFaceTexture] 얼굴 PNG가 너무 큽니다 ({png.Length / 1024} KB). 전송하지 않습니다.", this);
            return;
        }

        currentPng = png;
        ApplyPng(png);
        SendFace(PlayerRef.None);
    }

    /// <summary>target이 None이면 모든 프록시에게, 아니면 그 플레이어에게만 보낸다.</summary>
    private void SendFace(PlayerRef target)
    {
        if (currentPng == null)
            return;

        int id = ++sendId;
        int count = (currentPng.Length + ChunkSize - 1) / ChunkSize;
        for (int i = 0; i < count; i++)
        {
            int offset = i * ChunkSize;
            var chunk = new byte[Math.Min(ChunkSize, currentPng.Length - offset)];
            Buffer.BlockCopy(currentPng, offset, chunk, 0, chunk.Length);

            if (target == PlayerRef.None)
                RPC_FaceChunk(id, i, count, chunk);
            else
                RPC_FaceChunkTo(target, id, i, count, chunk);
        }
    }

    [Rpc(RpcSources.Proxies, RpcTargets.StateAuthority)]
    private void RPC_RequestFace(RpcInfo info = default)
    {
        SendFace(info.Source);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)]
    private void RPC_FaceChunk(int id, int index, int count, byte[] data)
    {
        ReceiveChunk(id, index, count, data);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_FaceChunkTo([RpcTarget] PlayerRef target, int id, int index, int count, byte[] data)
    {
        ReceiveChunk(id, index, count, data);
    }

    private void ReceiveChunk(int id, int index, int count, byte[] data)
    {
        if (id <= lastAppliedId || count <= 0 || index < 0 || index >= count)
            return;

        if (!incoming.TryGetValue(id, out byte[][] parts) || parts.Length != count)
        {
            parts = new byte[count][];
            incoming[id] = parts;
        }
        parts[index] = data;

        int total = 0;
        foreach (byte[] p in parts)
        {
            if (p == null)
                return; // 아직 다 안 옴
            total += p.Length;
        }

        var png = new byte[total];
        int offset = 0;
        foreach (byte[] p in parts)
        {
            Buffer.BlockCopy(p, 0, png, offset, p.Length);
            offset += p.Length;
        }

        lastAppliedId = id;
        // 이번 것보다 오래된 전송은 버린다
        var stale = new List<int>();
        foreach (int key in incoming.Keys)
            if (key <= id) stale.Add(key);
        foreach (int key in stale)
            incoming.Remove(key);

        currentPng = png;
        ApplyPng(png);
    }

    private void ApplyPng(byte[] png)
    {
        if (faceMaterial == null && !ResolveMaterial())
            return;

        if (faceTexture == null)
        {
            faceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = "NetworkFaceTexture",
                wrapMode = TextureWrapMode.Clamp,
            };
        }

        if (!faceTexture.LoadImage(png))
        {
            Debug.LogWarning("[NetworkFaceTexture] 얼굴 PNG 디코딩 실패", this);
            return;
        }

        FaceDrawShared.ApplyTexture(faceMaterial, faceTexture);
    }

    private bool ResolveMaterial()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<SkinnedMeshRenderer>(true);

        if (targetRenderer == null)
        {
            Debug.LogWarning("[NetworkFaceTexture] SkinnedMeshRenderer가 없습니다.", this);
            return false;
        }

        faceMaterial = targetRenderer.materials[faceMaterialIndex];
        FaceDrawShared.ProjectFaceUV(targetRenderer, faceMaterialIndex); // FaceDraw 씬과 같은 UV여야 그림 위치가 맞는다
        return true;
    }
}
