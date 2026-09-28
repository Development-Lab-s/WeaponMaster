using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;


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
    
    public bool IsDrawing { get; private set; }
    public Texture2D Texture => texture;
    public Color BackgroundColor => background[0];
    public bool CanUndo => undoStack.Count > 0;
    public bool InputLocked { get; set; }
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
    }
    private void SetCloseUp(bool closeUp)
    {
        int basePriority = mainCamera.Priority.Value;
        closeUpCamera.Priority = closeUp ? basePriority + 1 : basePriority - 1;
    }


    private void Update()
    {
        if (!IsDrawing || InputLocked || IsSaving || brain.IsBlending || Mouse.current == null) return;
        if (!Mouse.current.leftButton.isPressed)
        {
            pressing = false;
            return;
        }
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            pressing = true;
            undoSaved = false;
            hasLastPixel = false;
        }
        if (!pressing) return;
        
        if (TryGetFaceUV(outputCamera.ScreenPointToRay(Mouse.current.position.ReadValue()), out Vector2 uv))
        {
            if (!undoSaved)
            {
                PushUndo();
                undoSaved = true;
            }
            PaintTo(uv);
        }
        else
        {
            hasLastPixel = false;
        }
    }

    
    private bool TryGetFaceUV(Ray ray, out Vector2 uv)
    {
        uv = default;
        if (!meshCollider.Raycast(ray, out RaycastHit hit, 1000f))
            return false;
        if (hit.triangleIndex < faceTriStart || hit.triangleIndex >= faceTriEnd)
            return false;

        uv = hit.textureCoord;
        return true;
    }
    

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
        pressing = false;
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
