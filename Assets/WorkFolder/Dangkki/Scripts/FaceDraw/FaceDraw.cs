using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;


[DisallowMultipleComponent]
public class FaceDraw : MonoBehaviour
{
    [Header("대상")]
    [SerializeField] private SkinnedMeshRenderer targetRenderer;
    [SerializeField] private int faceMaterialIndex = 1;

    [Header("카메라")]
    [SerializeField] private CinemachineCamera mainCamera;
    [SerializeField] private CinemachineCamera closeUpCamera;

    [Header("텍스처")]
    [SerializeField] private int textureSize = 512;
    [SerializeField] private int maxUndo = 20;

    [Header("브러시")]
    public Color brushColor = Color.black;
    public float brushRadius = 6f;
    public bool eraser;
    

    [Header("이벤트")]
    public UnityEvent onDrawingStarted;
    public UnityEvent onDrawingEnded;
    public UnityEvent<Texture2D> onConfirmed;
    
    private Material faceMaterial;
    private Texture2D texture;
    private Color32[] pixels;
    private Color32[] background;
    private readonly List<Color32[]> undoStack = new List<Color32[]>();
    private bool dirty;
    
    private MeshCollider meshCollider;
    private Mesh colliderMesh;
    private int faceTriStart;
    private int faceTriEnd;
    
    private bool pressing; 
    private bool undoSaved;
    private bool hasLastPixel;
    private Vector2 lastPixel;
    
    private Camera outputCamera;
    private CinemachineBrain brain;
    private FaceBrushFeedback feedback;
    private Vector3[] bakedVertices;
    private Vector2[] bakedUV;
    private int[] bakedTriangles;
    private readonly List<RaycastResult> pointerHits = new();
    private float nextParticleTime;
    
    public bool IsDrawing { get; private set; }
    public Texture2D Texture => texture;
    public Color BackgroundColor => background[0];
    public bool CanUndo => undoStack.Count > 0;
    public bool InputLocked { get; set; }
    public bool SkipStartButton { get; set; }
    public System.Func<Texture2D, System.Threading.Tasks.Task> SaveAsync;
    public bool IsSaving { get; private set; }
    public string SaveError { get; private set; }
    private bool IsReady => texture != null && brain != null;


    private void Awake()
    {
        SetCloseUp(false);
        
        outputCamera = Camera.main;
        brain = outputCamera.GetComponent<CinemachineBrain>();
        
        
        faceMaterial = targetRenderer.materials[faceMaterialIndex];
        FaceDrawShared.ProjectFaceUV(targetRenderer, faceMaterialIndex);
        Color32 fill = FaceDrawShared.GetColor(faceMaterial);
        fill.a = 255;
        
        background = new Color32[textureSize * textureSize];
        for (int i = 0; i < background.Length; i++)
            background[i] = fill;
        pixels = (Color32[])background.Clone();
        
        texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
        { name = "FaceDrawTexture", wrapMode = TextureWrapMode.Clamp};
        texture.SetPixels32(pixels);
        texture.Apply(false);
        FaceDrawShared.ApplyTexture(faceMaterial, texture);
        
        GameObject go = new GameObject("FaceDrawCollider") { layer = 2, hideFlags = HideFlags.DontSave };
        go.transform.SetParent(targetRenderer.transform, false);
        meshCollider = go.AddComponent<MeshCollider>();

        SubMeshDescriptor sub = targetRenderer.sharedMesh.GetSubMesh(faceMaterialIndex);
        faceTriStart = sub.indexStart / 3;
        faceTriEnd = (sub.indexStart + sub.indexCount) / 3;
    }

    private void OnDestroy()
    {
        if (feedback != null) Destroy(feedback.transform.parent.gameObject);
        if (texture != null) Destroy(texture);
        if (faceMaterial != null) Destroy(faceMaterial);
        if (colliderMesh != null) Destroy(colliderMesh);
    }
    

    
    
    private void BakeCollider()
    {
        if (colliderMesh == null)
            colliderMesh = new Mesh { name = "FaceDrawBaked", hideFlags = HideFlags.DontSave };
        targetRenderer.BakeMesh(colliderMesh);
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = colliderMesh;
        bakedVertices = colliderMesh.vertices;
        bakedUV = colliderMesh.uv;
        bakedTriangles = colliderMesh.triangles;
    }
    private void SetCloseUp(bool closeUp)
    {
        int basePriority = mainCamera.Priority.Value;
        closeUpCamera.Priority = closeUp ? basePriority + 1 : basePriority - 1;
    }


    private void Update()
    {
        if (!IsDrawing || InputLocked || IsSaving || brain == null || brain.IsBlending ||
            Mouse.current == null || !Application.isFocused)
        {
            ResetPointer();
            return;
        }
        Vector2 pointer = Mouse.current.position.ReadValue();
        if (IsPointerOverControls(pointer))
        {
            ResetPointer();
            return;
        }
        bool onFace = TryGetFaceHit(outputCamera.ScreenPointToRay(pointer), out RaycastHit hit);
        if (onFace)
        {
            if (feedback == null) feedback = FaceBrushFeedback.Create(transform);
            feedback.Show(pointer, GetScreenBrushRadius(hit));
        }
        else if (feedback != null) feedback.Hide(false);
        if (!Mouse.current.leftButton.isPressed)
        {
            pressing = false;
            hasLastPixel = false;
            return;
        }
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            pressing = true;
            undoSaved = false;
            hasLastPixel = false;
        }
        if (!pressing) return;
        
        if (onFace)
        {
            if (!undoSaved)
            {
                PushUndo();
                undoSaved = true;
            }
            PaintTo(hit.textureCoord);
            if (Time.unscaledTime >= nextParticleTime)
            {
                feedback.Emit(outputCamera.WorldToScreenPoint(hit.point), eraser ? BackgroundColor : brushColor);
                nextParticleTime = Time.unscaledTime + 1f / 30f;
            }
        }
        else
        {
            hasLastPixel = false;
        }
    }

    
    private bool TryGetFaceHit(Ray ray, out RaycastHit hit)
    {
        if (!meshCollider.Raycast(ray, out hit, 1000f))
            return false;
        if (hit.triangleIndex < faceTriStart || hit.triangleIndex >= faceTriEnd)
            return false;

        return true;
    }

    private bool IsPointerOverControls(Vector2 point)
    {
        if (EventSystem.current == null) return false;
        pointerHits.Clear();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, pointerHits);
        foreach (var hit in pointerHits)
        {
            if (hit.gameObject.GetComponentInParent<Selectable>() != null ||
                hit.gameObject.GetComponentInParent<FaceColorPicker>() != null) return true;
            // The full-screen fade also blocks raw painting input while switching panels.
            var canvas = hit.gameObject.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.sortingOrder == short.MaxValue) return true;
        }
        return false;
    }

    private float GetScreenBrushRadius(RaycastHit hit)
    {
        int triangle = hit.triangleIndex * 3;
        int a = bakedTriangles[triangle], b = bakedTriangles[triangle + 1], c = bakedTriangles[triangle + 2];
        Vector2 uv1 = bakedUV[b] - bakedUV[a], uv2 = bakedUV[c] - bakedUV[a];
        float determinant = uv1.x * uv2.y - uv1.y * uv2.x;
        if (Mathf.Abs(determinant) < 0.000001f) return 3f;
        Vector3 edge1 = bakedVertices[b] - bakedVertices[a], edge2 = bakedVertices[c] - bakedVertices[a];
        float radiusUV = brushRadius / textureSize;
        Vector3 du = meshCollider.transform.TransformVector((edge1 * uv2.y - edge2 * uv1.y) / determinant) * radiusUV;
        Vector3 dv = meshCollider.transform.TransformVector((edge2 * uv1.x - edge1 * uv2.x) / determinant) * radiusUV;
        Vector2 center = outputCamera.WorldToScreenPoint(hit.point);
        float radiusX = Vector2.Distance(center, outputCamera.WorldToScreenPoint(hit.point + du));
        float radiusY = Vector2.Distance(center, outputCamera.WorldToScreenPoint(hit.point + dv));
        return Mathf.Max(1f, (radiusX + radiusY) * 0.5f);
    }

    private void ResetPointer()
    {
        pressing = false;
        hasLastPixel = false;
        if (feedback != null) feedback.Hide(true);
    }

    private void OnDisable() => ResetPointer();
    

    private void PaintTo(Vector2 uv)
    {
        Vector2 p = uv * textureSize;

        if (hasLastPixel)
        {
            float step = Mathf.Max(1f, brushRadius * 0.3f);
            int count = Mathf.CeilToInt(Vector2.Distance(lastPixel, p) / step);
            for (int i = 1; i <= count; i++)
                Stamp(Vector2.Lerp(lastPixel, p, i / (float)count));
        }
        else
        {
            Stamp(p);
        }

        lastPixel = p;
        hasLastPixel = true;
    }
    
    private void Stamp(Vector2 center)
    {
        float r = brushRadius;
        int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - r - 1));
        int maxX = Mathf.Min(textureSize - 1, Mathf.CeilToInt(center.x + r + 1));
        int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - r - 1));
        int maxY = Mathf.Min(textureSize - 1, Mathf.CeilToInt(center.y + r + 1));

        Color32 paint = brushColor;
        paint.a = 255;
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float a = Mathf.Clamp01(r - d + 0.5f);
                if (a <= 0f)
                    continue;

                int i = y * textureSize + x;
                pixels[i] = Color32.Lerp(pixels[i], eraser ? background[i] : paint, a);
            }
        }
        dirty = true;
    }

    private void LateUpdate()
    {
        if (!dirty)
            return;

        dirty = false;
        texture.SetPixels32(pixels);
        texture.Apply(false);
    }

    private void PushUndo()
    {
        undoStack.Add((Color32[])pixels.Clone());
        if (undoStack.Count > maxUndo)
            undoStack.RemoveAt(0);
    }

    // ############################################################ UI 호출 부분

    public void BeginDrawing()
    {
        if (IsDrawing || !IsReady || InputLocked || IsSaving)
            return;

        BakeCollider();
        SetCloseUp(true);
        IsDrawing = true;
        onDrawingStarted?.Invoke();
    }

    public async void Confirm()
    {
        if (!IsDrawing || InputLocked || IsSaving)
            return;
        IsSaving = true;
        SaveError = null;
        try
        {
            texture.SetPixels32(pixels);
            texture.Apply(false);
            if (SaveAsync != null) await SaveAsync(texture);
            if (this == null) return;
            byte[] png = texture.EncodeToPNG();
            FaceDrawShared.SetFace(png);
            undoStack.Clear();
            onConfirmed?.Invoke(texture);
            EndDrawing();
        }
        catch (System.Exception ex)
        {
            SaveError = "얼굴 저장 실패. 다시 저장해주세요.";
            Debug.LogException(ex, this);
        }
        finally { IsSaving = false; }
    }
    
    public void Cancel()
    {
        if (IsSaving) return;
        if (IsDrawing)
            EndDrawing();
    }

    private void EndDrawing()
    {
        ResetPointer();
        SetCloseUp(false);
        IsDrawing = false;
        onDrawingEnded?.Invoke();
    }

    public void Undo() // 한번 되돌리는거
    {
        if (InputLocked || IsSaving) return;
        if (undoStack.Count == 0)
            return;

        pixels = undoStack[undoStack.Count - 1];
        undoStack.RemoveAt(undoStack.Count - 1);
        dirty = true;
    }

    public void Clear() // 다 치우기
    {
        if (InputLocked || IsSaving) return;
        PushUndo();
        pixels = (Color32[])background.Clone();
        dirty = true;
    }
    
    public void SetColor(Color color) { brushColor = color; eraser = false; }
    public void SetBrushRadius(float radius) => brushRadius = radius;
    public void SetEraser(bool on) => eraser = on;

    public void InitializeCanvas(Material skin, byte[] savedPng, Color? savedBackground = null)
    {
        SaveError = null;
        // Keep the body and face shading consistent with the current editor selection.
        var materials = targetRenderer.sharedMaterials;
        materials[0] = skin;
        materials[faceMaterialIndex] = faceMaterial;
        targetRenderer.sharedMaterials = materials;
        faceMaterial.shader = skin.shader;
        faceMaterial.CopyPropertiesFromMaterial(skin);
        FaceDrawShared.ApplyTexture(faceMaterial, texture);
        Color32 fill = FaceDrawShared.GetColor(skin);
        fill.a = 255;
        for (int i = 0; i < background.Length; i++) background[i] = fill;
        pixels = (Color32[])background.Clone();
        if (savedPng != null)
        {
            if (!texture.LoadImage(savedPng) || texture.width != textureSize || texture.height != textureSize)
                throw new System.InvalidOperationException("Saved face dimensions do not match the canvas.");
            FaceDrawShared.ApplySkinBackground(texture, savedBackground ?? Color.white, fill);
            pixels = texture.GetPixels32();
        }
        texture.SetPixels32(pixels);
        texture.Apply(false);
        undoStack.Clear();
        dirty = false;
    }
    
    // ############################################################ UI 호출 끝
}

// Runtime-only UI feedback. Fixed particle storage avoids per-stroke objects/materials.
internal sealed class FaceBrushFeedback : MaskableGraphic
{
    private struct Spark
    {
        public Vector2 position, velocity;
        public Color color;
        public float age, lifetime, size;
    }

    private readonly Spark[] sparks = new Spark[64];
    private int sparkCount;
    private bool showRing;
    private Vector2 pointer;
    private float radius;

    public static FaceBrushFeedback Create(Transform owner)
    {
        var root = new GameObject("Face Brush Feedback", typeof(RectTransform), typeof(Canvas));
        root.transform.SetParent(owner, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = short.MaxValue - 1;
        var graphic = new GameObject("Brush Ring And Paint Sparks", typeof(RectTransform), typeof(CanvasRenderer));
        graphic.transform.SetParent(root.transform, false);
        var rect = (RectTransform)graphic.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var feedback = graphic.AddComponent<FaceBrushFeedback>();
        feedback.raycastTarget = false;
        return feedback;
    }

    public void Show(Vector2 screenPoint, float screenRadius)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, null, out pointer);
        radius = screenRadius;
        showRing = true;
        SetVerticesDirty();
    }

    public void Hide(bool clearParticles)
    {
        if (!showRing && (!clearParticles || sparkCount == 0)) return;
        showRing = false;
        if (clearParticles) sparkCount = 0;
        SetVerticesDirty();
    }

    public void Emit(Vector2 screenPoint, Color paintColor)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, null, out Vector2 origin);
        for (int i = 0; i < 2 && sparkCount < sparks.Length; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            sparks[sparkCount++] = new Spark
            {
                position = origin,
                velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(20f, 55f),
                color = Color.Lerp(paintColor, Color.white, 0.25f),
                lifetime = Random.Range(0.18f, 0.32f),
                size = Random.Range(1.5f, 3f)
            };
        }
        SetVerticesDirty();
    }

    private void Update()
    {
        if (sparkCount == 0) return;
        float delta = Time.unscaledDeltaTime;
        for (int i = sparkCount - 1; i >= 0; i--)
        {
            Spark spark = sparks[i];
            spark.age += delta;
            if (spark.age >= spark.lifetime)
            {
                sparks[i] = sparks[--sparkCount];
                continue;
            }
            spark.position += spark.velocity * delta;
            spark.velocity += Vector2.down * (45f * delta);
            sparks[i] = spark;
        }
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        for (int i = 0; i < sparkCount; i++)
        {
            Spark spark = sparks[i];
            float remaining = 1f - spark.age / spark.lifetime;
            Color tint = spark.color;
            tint.a = remaining;
            Ring(vh, spark.position, 0f, spark.size * remaining + 0.8f, new Color(0f, 0f, 0f, remaining * 0.6f), 8);
            Ring(vh, spark.position, 0f, spark.size * remaining, tint, 8);
        }
        if (!showRing) return;
        // Two contrasting edges stay legible on both dark paint and pale skin.
        Ring(vh, pointer, Mathf.Max(0f, radius - 1.5f), radius + 1.5f, Color.black, 64);
        Ring(vh, pointer, Mathf.Max(0f, radius - 0.6f), radius + 0.6f, Color.white, 64);
    }

    private static void Ring(VertexHelper vh, Vector2 center, float inner, float outer, Color tint, int segments)
    {
        int start = vh.currentVertCount;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            vh.AddVert(center + direction * inner, tint, Vector2.one * 0.5f);
            vh.AddVert(center + direction * outer, tint, Vector2.one * 0.5f);
            if (i == segments) continue;
            int vertex = start + i * 2;
            vh.AddTriangle(vertex, vertex + 1, vertex + 3);
            vh.AddTriangle(vertex, vertex + 3, vertex + 2);
        }
    }
}
