using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// 몸 그리기의 "종이". 라이트맵 UV(uv2) 공간의 텍스처 픽셀마다 몸의 어느 삼각형 어느 지점인지 미리 구워 둔다.
/// 그리기 시작할 때 현재 포즈로 픽셀별 3D 위치를 계산해서(UpdatePose), 브러시를 3D 구로 찍는다.
/// 그래서 UV 조각 경계에서 선이 끊기지 않고, 레이캐스트는 맞은 "지점"만 쓰면 된다 (삼각형 번호에 의존하지 않음).
/// </summary>
public class BodyPaintSurface
{
    private const int GridResolution = 48;

    public readonly int Size;
    /// <summary>텍스처 중 몸이 차지하는 비율 (0~1). 낮으면 라이트맵 UV가 작게 몰려 있어 그림이 뭉개진다.</summary>
    public readonly float Coverage;
    public float Height { get; private set; } // 현재 포즈 기준 몸 크기 (브러시 크기 기준)

    private readonly int[] triangles;
    private readonly bool[] covered;       // 몸이 찍힌 픽셀인지
    private readonly int[] texelTriangle;  // 픽셀이 속한 삼각형
    private readonly Vector3[] texelBary;  // 그 삼각형 안의 무게중심 좌표
    private readonly int[] dilateSource;   // 빈 픽셀이 색을 빌려올 옆 픽셀 (UV 조각 경계 번짐 방지)

    // UpdatePose에서 채움
    private Vector3[] texelPosition;
    private Vector3[] texelNormal;
    private Vector3 gridMin;
    private float cellSize;
    private int gx, gy, gz;
    private int[] cellStart;
    private int[] cellTexels;

    public BodyPaintSurface(SkinnedMeshRenderer renderer, int faceSubMesh, int size)
    {
        Mesh mesh = renderer.sharedMesh;
        Size = size;
        triangles = mesh.triangles;

        var uv2 = new List<Vector2>();
        mesh.GetUVs(1, uv2);
        if (uv2.Count != mesh.vertexCount)
            throw new InvalidOperationException($"'{mesh.name}'에 라이트맵 UV(uv2)가 없습니다. FBX 임포트 설정에서 Generate Lightmap UVs를 켜 주세요.");

        // 얼굴 서브메시 삼각형은 제외
        var isBody = new bool[triangles.Length / 3];
        int tri = 0;
        for (int s = 0; s < mesh.subMeshCount; s++)
        {
            int count = (int)mesh.GetIndexCount(s) / 3;
            for (int k = 0; k < count; k++)
                isBody[tri++] = s != faceSubMesh;
        }

        int texelCount = size * size;
        covered = new bool[texelCount];
        texelTriangle = new int[texelCount];
        texelBary = new Vector3[texelCount];

        Rasterize(uv2, isBody);
        dilateSource = BuildDilation();

        int coveredCount = 0;
        foreach (bool c in covered)
            if (c) coveredCount++;
        Coverage = coveredCount / (float)texelCount;
    }

    /// <summary>삼각형을 UV 공간에 찍어서 픽셀마다 (삼각형, 무게중심)을 기록</summary>
    private void Rasterize(List<Vector2> uv2, bool[] isBody)
    {
        for (int t = 0; t < isBody.Length; t++)
        {
            if (!isBody[t])
                continue;

            int i0 = triangles[t * 3], i1 = triangles[t * 3 + 1], i2 = triangles[t * 3 + 2];
            Vector2 a = uv2[i0] * Size - Vector2.one * 0.5f; // 픽셀 중심 기준
            Vector2 b = uv2[i1] * Size - Vector2.one * 0.5f;
            Vector2 c = uv2[i2] * Size - Vector2.one * 0.5f;

            float area = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
            if (Mathf.Abs(area) < 1e-8f)
                continue;

            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
            int maxX = Mathf.Min(Size - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
            int maxY = Mathf.Min(Size - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    // 무게중심 좌표
                    float w1 = ((x - a.x) * (c.y - a.y) - (y - a.y) * (c.x - a.x)) / area;
                    float w2 = ((b.x - a.x) * (y - a.y) - (b.y - a.y) * (x - a.x)) / area;
                    float w0 = 1f - w1 - w2;
                    const float eps = -0.02f; // 얇은 삼각형도 조금 덮이게
                    if (w0 < eps || w1 < eps || w2 < eps)
                        continue;

                    int i = y * Size + x;
                    covered[i] = true;
                    texelTriangle[i] = t;
                    texelBary[i] = new Vector3(w0, w1, w2);
                }
            }
        }
    }

    /// <summary>빈 픽셀 중 몸 픽셀 근처(3px)에 있는 것은 가장 가까운 몸 픽셀 색을 쓰게 한다</summary>
    private int[] BuildDilation()
    {
        int n = Size * Size;
        var source = new int[n];
        for (int i = 0; i < n; i++)
            source[i] = covered[i] ? i : -1;

        for (int pass = 0; pass < 3; pass++)
        {
            var next = (int[])source.Clone();
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int i = y * Size + x;
                    if (source[i] >= 0)
                        continue;
                    if (x > 0 && source[i - 1] >= 0) next[i] = source[i - 1];
                    else if (x < Size - 1 && source[i + 1] >= 0) next[i] = source[i + 1];
                    else if (y > 0 && source[i - Size] >= 0) next[i] = source[i - Size];
                    else if (y < Size - 1 && source[i + Size] >= 0) next[i] = source[i + Size];
                }
            }
            source = next;
        }
        return source;
    }

    /// <summary>
    /// 현재 포즈의 정점(BakeMesh 결과, 렌더러 로컬 공간)으로 픽셀별 3D 위치/법선과 검색 격자를 다시 만든다.
    /// 그리기 시작할 때마다 호출.
    /// </summary>
    public void UpdatePose(Mesh bakedMesh)
    {
        Vector3[] vertices = bakedMesh.vertices;
        Vector3[] normals = bakedMesh.normals;
        int texelCount = Size * Size;
        if (texelPosition == null)
        {
            texelPosition = new Vector3[texelCount];
            texelNormal = new Vector3[texelCount];
        }

        for (int i = 0; i < texelCount; i++)
        {
            if (!covered[i])
                continue;
            int t = texelTriangle[i];
            int i0 = triangles[t * 3], i1 = triangles[t * 3 + 1], i2 = triangles[t * 3 + 2];
            Vector3 w = texelBary[i];
            texelPosition[i] = vertices[i0] * w.x + vertices[i1] * w.y + vertices[i2] * w.z;
            texelNormal[i] = (normals[i0] * w.x + normals[i1] * w.y + normals[i2] * w.z).normalized;
        }

        // 격자 (카운팅 정렬)
        Bounds b = bakedMesh.bounds;
        Height = Mathf.Max(b.size.x, b.size.y, b.size.z);
        cellSize = Height / GridResolution;
        gridMin = b.min - Vector3.one * cellSize;
        gx = Mathf.CeilToInt(b.size.x / cellSize) + 3;
        gy = Mathf.CeilToInt(b.size.y / cellSize) + 3;
        gz = Mathf.CeilToInt(b.size.z / cellSize) + 3;

        var texelCell = new int[texelCount];
        cellStart = new int[gx * gy * gz + 1];
        int coveredCount = 0;
        for (int i = 0; i < texelCount; i++)
        {
            if (!covered[i]) { texelCell[i] = -1; continue; }
            int c = CellOf(texelPosition[i]);
            texelCell[i] = c;
            cellStart[c + 1]++;
            coveredCount++;
        }
        for (int c = 0; c < gx * gy * gz; c++)
            cellStart[c + 1] += cellStart[c];

        cellTexels = new int[coveredCount];
        var fill = (int[])cellStart.Clone();
        for (int i = 0; i < texelCount; i++)
            if (texelCell[i] >= 0)
                cellTexels[fill[texelCell[i]]++] = i;
    }

    private int CellOf(Vector3 p)
    {
        int x = Mathf.Clamp((int)((p.x - gridMin.x) / cellSize), 0, gx - 1);
        int y = Mathf.Clamp((int)((p.y - gridMin.y) / cellSize), 0, gy - 1);
        int z = Mathf.Clamp((int)((p.z - gridMin.z) / cellSize), 0, gz - 1);
        return (z * gy + y) * gx + x;
    }

    /// <summary>
    /// 3D 구 브러시 한 번 찍기. 중심에서 radius 안 + 같은 쪽을 향한 면만 칠한다
    /// (얇은 팔 반대편까지 뚫고 칠해지지 않게). 바뀐 픽셀 범위를 dirty에 합친다.
    /// </summary>
    public void Stamp(Color32[] paint, Vector3 center, Vector3 normal, float radius,
                      Color32 color, bool erase, ref RectInt dirty)
    {
        float feather = Mathf.Max(radius * 0.2f, 1e-5f);
        Vector3 min = center - Vector3.one * radius;
        Vector3 max = center + Vector3.one * radius;
        int x0 = Mathf.Clamp((int)((min.x - gridMin.x) / cellSize), 0, gx - 1), x1 = Mathf.Clamp((int)((max.x - gridMin.x) / cellSize), 0, gx - 1);
        int y0 = Mathf.Clamp((int)((min.y - gridMin.y) / cellSize), 0, gy - 1), y1 = Mathf.Clamp((int)((max.y - gridMin.y) / cellSize), 0, gy - 1);
        int z0 = Mathf.Clamp((int)((min.z - gridMin.z) / cellSize), 0, gz - 1), z1 = Mathf.Clamp((int)((max.z - gridMin.z) / cellSize), 0, gz - 1);
        float r2 = radius * radius;

        for (int z = z0; z <= z1; z++)
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            int cell = (z * gy + y) * gx + x;
            for (int k = cellStart[cell]; k < cellStart[cell + 1]; k++)
            {
                int i = cellTexels[k];
                float d2 = (texelPosition[i] - center).sqrMagnitude;
                if (d2 > r2 || Vector3.Dot(texelNormal[i], normal) < 0.1f)
                    continue;

                float a = Mathf.Clamp01((radius - Mathf.Sqrt(d2)) / feather);
                Color32 old = paint[i];
                if (erase)
                {
                    old.a = (byte)Mathf.RoundToInt(old.a * (1f - a));
                    paint[i] = old;
                }
                else
                {
                    Color32 mixed = old.a == 0 ? color : Color32.Lerp(old, color, a);
                    mixed.a = (byte)Mathf.RoundToInt(Mathf.Lerp(old.a, 255f, a));
                    paint[i] = mixed;
                }

                dirty = Encapsulate(dirty, i % Size, i / Size);
            }
        }
    }

    private static RectInt Encapsulate(RectInt r, int x, int y)
    {
        if (r.width <= 0) return new RectInt(x, y, 1, 1);
        int xMin = Mathf.Min(r.xMin, x), yMin = Mathf.Min(r.yMin, y);
        int xMax = Mathf.Max(r.xMax, x + 1), yMax = Mathf.Max(r.yMax, y + 1);
        return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
    }

    public RectInt FullRect => new RectInt(0, 0, Size, Size);

    /// <summary>피부색 위에 그림 레이어를 얹어서 화면용 픽셀을 만든다 (region만)</summary>
    public void Composite(Color32[] paint, Color32 skin, Color32[] output, RectInt region)
    {
        // 경계 번짐용으로 3px 넓혀서 처리
        int xMin = Mathf.Max(0, region.xMin - 3), yMin = Mathf.Max(0, region.yMin - 3);
        int xMax = Mathf.Min(Size, region.xMax + 3), yMax = Mathf.Min(Size, region.yMax + 3);

        for (int y = yMin; y < yMax; y++)
        for (int x = xMin; x < xMax; x++)
        {
            int i = y * Size + x;
            if (!covered[i])
                continue;
            Color32 p = paint[i];
            Color32 c = Color32.Lerp(skin, p, p.a / 255f);
            c.a = 255;
            output[i] = c;
        }

        for (int y = yMin; y < yMax; y++)
        for (int x = xMin; x < xMax; x++)
        {
            int i = y * Size + x;
            if (covered[i])
                continue;
            int s = dilateSource[i];
            output[i] = s >= 0 ? output[s] : skin;
        }
    }

    // ───────── 머티리얼 ─────────

    /// <summary>얼굴을 뺀 몸 머티리얼들을 몸 그리기 셰이더 + 텍스처로 바꾸고, 원래 피부색을 돌려준다.</summary>
    public static Color32 SetupBodyMaterials(SkinnedMeshRenderer renderer, int faceSubMesh, Shader shader, Texture texture)
    {
        if (shader == null || !shader.isSupported)
            Debug.LogError("[BodyPaint] 몸 그리기 셰이더(Dangkki/BodyPaintLit)가 없거나 지원되지 않습니다. " +
                           "셰이더 파일을 우클릭 > Reimport 해 주세요. 이대로면 그림이 원래 UV로 그려져 번집니다.", renderer);

        Material[] materials = renderer.materials; // 인스턴스
        Color skin = Color.white;
        bool first = true;
        for (int m = 0; m < materials.Length; m++)
        {
            if (m == faceSubMesh)
                continue;
            if (first)
            {
                skin = FaceDrawShared.GetColor(materials[m]);
                first = false;
            }
            materials[m].shader = shader;
            FaceDrawShared.ApplyTexture(materials[m], texture);
        }
        Color32 result = skin;
        result.a = 255;
        return result;
    }
}

/// <summary>
/// 몸 그림 파일 저장/불러오기 + "지금 적용된 그림" 기록.
/// 위치: {persistentDataPath}/BodyPaint/{이름}.png, 적용 기록은 BodyPaint/applied.txt
/// </summary>
public static class BodyPaintStorage
{
    public static string Folder => Path.Combine(Application.persistentDataPath, "BodyPaint");
    private static string AppliedPath => Path.Combine(Folder, "applied.txt");
    private static string FilePath(string name) => Path.Combine(Folder, name + ".png");

    /// <summary>파일 이름으로 못 쓰는 문자 제거</summary>
    public static string CleanName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c.ToString(), "");
        name = name.Trim();
        return name.Length == 0 ? null : name;
    }

    /// <summary>저장된 그림 이름들 (최근 것 먼저)</summary>
    public static string[] List()
    {
        if (!Directory.Exists(Folder))
            return Array.Empty<string>();
        return new DirectoryInfo(Folder).GetFiles("*.png")
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .Select(f => Path.GetFileNameWithoutExtension(f.Name))
            .ToArray();
    }

    public static void Save(string name, byte[] png)
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllBytes(FilePath(name), png);
    }

    public const string AssetFolder = "Assets/WorkFolder/Dangkki/BodyPaints";

    /// <summary>
    /// 에디터 전용: Assets/WorkFolder/Dangkki/BodyPaints 에 {이름}.png + {이름}.mat 을 만든다.
    /// 이 머티리얼을 같은 모델의 몸(피부) 슬롯에 넣으면 그림이 입혀진다. 빌드된 게임에서는 아무것도 안 함.
    /// </summary>
    public static void SaveMaterialAsset(string name, byte[] finalPng, Shader shader)
    {
#if UNITY_EDITOR
        Directory.CreateDirectory(AssetFolder);
        string texPath = $"{AssetFolder}/{name}.png";
        File.WriteAllBytes(texPath, finalPng);
        UnityEditor.AssetDatabase.ImportAsset(texPath);

        var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(texPath);
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = false;
        importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed; // 압축하면 UV 조각 경계가 번짐
        importer.SaveAndReimport();

        string matPath = $"{AssetFolder}/{name}.mat";
        var material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (material == null)
        {
            material = new Material(shader);
            UnityEditor.AssetDatabase.CreateAsset(material, matPath);
        }
        material.shader = shader;
        FaceDrawShared.ApplyTexture(material, UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
        UnityEditor.EditorUtility.SetDirty(material);
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log($"[BodyPaint] 머티리얼 저장: {matPath}");
#endif
    }

    public static byte[] Load(string name)
    {
        string path = FilePath(name);
        return File.Exists(path) ? File.ReadAllBytes(path) : null;
    }

    public static void Delete(string name)
    {
        string path = FilePath(name);
        if (File.Exists(path))
            File.Delete(path);
        if (Applied == name)
            Applied = null;
    }

    /// <summary>지금 몸에 적용된 그림 이름 (없으면 null)</summary>
    public static string Applied
    {
        get
        {
            if (!File.Exists(AppliedPath))
                return null;
            string name = File.ReadAllText(AppliedPath).Trim();
            return name.Length == 0 ? null : name;
        }
        set
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(AppliedPath, value ?? "");
        }
    }

    /// <summary>PNG를 픽셀로 (크기가 다르면 null)</summary>
    public static Color32[] Decode(byte[] png, int size)
    {
        if (png == null)
            return null;
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!tex.LoadImage(png) || tex.width != size || tex.height != size)
                return null;
            return tex.GetPixels32();
        }
        finally
        {
            UnityEngine.Object.Destroy(tex);
        }
    }

    public static byte[] Encode(Color32[] pixels, int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        try
        {
            tex.SetPixels32(pixels);
            tex.Apply(false);
            return tex.EncodeToPNG();
        }
        finally
        {
            UnityEngine.Object.Destroy(tex);
        }
    }

    /// <summary>적용된 그림을 픽셀로 (없으면 빈 레이어)</summary>
    public static Color32[] LoadApplied(int size)
    {
        string name = Applied;
        Color32[] pixels = name != null ? Decode(Load(name), size) : null;
        return pixels ?? new Color32[size * size];
    }
}
