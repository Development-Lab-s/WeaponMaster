using System;
using System.Collections.Generic;
using UnityEngine;


public static class FaceDrawShared
{

    public static byte[] Png { get; private set; }
    public static bool HasFace => Png != null && Png.Length > 0;
    public static event Action<byte[]> OnFaceChanged;
    public static void SetFace(byte[] png)
    {
        Png = png;
        OnFaceChanged?.Invoke(png);
    }
    

    private const float UvMargin = 0.02f;
    private static readonly Dictionary<(Mesh, int), Mesh> ProjectedMeshes = new Dictionary<(Mesh, int), Mesh>();
    
    public static void ProjectFaceUV(SkinnedMeshRenderer renderer, int faceSubMesh)
    {
        Mesh source = renderer.sharedMesh;
        if (source == null || ProjectedMeshes.ContainsValue(source)) return;
        

        if (!ProjectedMeshes.TryGetValue((source, faceSubMesh), out Mesh projected) || projected == null)
        {
            Vector3 localUp = renderer.transform.InverseTransformDirection(Vector3.up);
            projected = BuildProjectedMesh(source, faceSubMesh, localUp);
            ProjectedMeshes[(source, faceSubMesh)] = projected;
        }

        renderer.sharedMesh = projected;
    }

    private static Mesh BuildProjectedMesh(Mesh source, int faceSubMesh, Vector3 upHint) // 계산은 깡통이 해줌;;나머지는 내가함
    {
        Mesh mesh = UnityEngine.Object.Instantiate(source);
        mesh.name = source.name + " (FaceUV)";

        Vector3[] verts = mesh.vertices;
        Vector3[] normals = mesh.normals;
        HashSet<int> faceVerts = new HashSet<int>(mesh.GetIndices(faceSubMesh));

        List<Vector2> uvs = new List<Vector2>();
        mesh.GetUVs(0, uvs);
        if (uvs.Count != verts.Length)
            uvs = new List<Vector2>(new Vector2[verts.Length]);
        
        Vector3 forward = Vector3.zero;
        Vector3 center = Vector3.zero;
        foreach (int i in faceVerts)
        {
            if (normals.Length == verts.Length)
                forward += normals[i];
            center += verts[i];
        }
        center /= Mathf.Max(1, faceVerts.Count);
        forward = forward.sqrMagnitude > 1e-6f ? forward.normalized : Vector3.forward;
        
        Vector3 up = Vector3.ProjectOnPlane(upHint, forward);
        if (up.sqrMagnitude < 1e-6f)
            up = Vector3.ProjectOnPlane(Vector3.up, forward);
        up.Normalize();
        Vector3 right = Vector3.Cross(up, -forward).normalized;

        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
        foreach (int i in faceVerts)
        {
            Vector3 d = verts[i] - center;
            float x = Vector3.Dot(d, right);
            float y = Vector3.Dot(d, up);
            minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
            minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
        }
        
        float scale = (1f - UvMargin * 2f) / Mathf.Max(maxX - minX, maxY - minY, 1e-5f);
        float midX = (minX + maxX) * 0.5f;
        float midY = (minY + maxY) * 0.5f;

        foreach (int i in faceVerts)
        {
            Vector3 d = verts[i] - center;
            uvs[i] = new Vector2(
                (Vector3.Dot(d, right) - midX) * scale + 0.5f,
                (Vector3.Dot(d, up) - midY) * scale + 0.5f);
        }

        mesh.SetUVs(0, uvs);
        return mesh;
    }
    
    private static readonly string[] TextureProps = { "_BaseMap", "baseColorTexture", "_MainTex" };
    private static readonly string[] ColorProps = { "_BaseColor", "baseColorFactor", "_Color" };

    public static Color GetColor(Material material)
    {
        foreach (string prop in ColorProps)
        {
            if (material.HasProperty(prop))
                return material.GetColor(prop);
        }
        return Color.white;
    }
    
    public static void ApplyTexture(Material material, Texture texture)
    {
        foreach (string prop in TextureProps)
        {
            if (!material.HasProperty(prop))
                continue;
            material.SetTexture(prop, texture);
            material.SetTextureScale(prop, Vector2.one);
            material.SetTextureOffset(prop, Vector2.zero);
        }

        foreach (string prop in ColorProps)
        {
            if (material.HasProperty(prop))
                material.SetColor(prop, Color.white);
        }
    }
}
