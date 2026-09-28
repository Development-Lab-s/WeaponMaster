using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 몸 전체에 그림 그리기. 캐릭터에 붙인다.
/// 흐름: 그리기 시작(그리기 카메라로 전환) → 그리기 (오른쪽 드래그로 캐릭터 회전) → 확정(이름 붙여 파일 저장 + 적용) / 취소(마지막 확정 상태로 되돌림)
/// 확정한 그림은 다른 씬에서 BodyPaintApplier가 불러다 입힌다.
/// </summary>
[DisallowMultipleComponent]
public class BodyDraw : MonoBehaviour
{
    [Header("대상")]
    [SerializeField] private SkinnedMeshRenderer targetRenderer;
    [SerializeField] private int faceMaterialIndex = 1;
    [SerializeField] private Shader bodyShader;
    [SerializeField] private int textureSize = 1024;
    [SerializeField] private int maxUndo = 10;

    [Header("카메라 (시네머신)")]
    [SerializeField] private CinemachineCamera overviewCamera;
    [SerializeField] private CinemachineCamera drawCamera;

    [Header("브러시")]
    public Color brushColor = Color.black;
    [Tooltip("1 = 키의 0.2%")]
    [Range(1f, 64f)] public float brushSize = 6f;
    public bool eraser;
    [Tooltip("오른쪽 드래그 회전 속도 (도/픽셀)")]
    [SerializeField] private float rotateSpeed = 0.4f;

    [Header("이벤트")]
    public UnityEvent onDrawingStarted;
    public UnityEvent onDrawingEnded;

    private BodyPaintSurface surface;
    private Color32[] paint;     // 그림 레이어 (알파 = 칠해진 정도)
    private Color32[] display;   // 피부색 + 그림 합친 화면용
    private Color32 skinColor;
    private Texture2D texture;
    private RectInt dirty;
    private readonly List<Color32[]> undoStack = new List<Color32[]>();

    private MeshCollider meshCollider;
    private Mesh colliderMesh;
    private Camera outputCamera;
    private CinemachineBrain brain;
    private Quaternion startRotation;

    private bool pressing;
    private bool undoSaved;
    private bool hasLastPoint;
    private Vector3 lastPoint;

    public bool IsDrawing { get; private set; }
    public bool CanUndo => undoStack.Count > 0;
    public bool IsReady => surface != null;

    // ───────── 초기화 ─────────

    private void Awake()
    {
        outputCamera = Camera.main;
        brain = outputCamera.GetComponent<CinemachineBrain>();

        try
        {
            surface = new BodyPaintSurface(targetRenderer, faceMaterialIndex, textureSize);
        }
        catch (System.InvalidOperationException e)
        {
            Debug.LogError("[BodyDraw] " + e.Message, this);
            return;
        }

        // 몸이 텍스처를 얼마나 쓰는지. 너무 낮으면 라이트맵 UV가 작게 몰려서 그림이 계단처럼 뭉개진다.
        Debug.Log($"[BodyDraw] 텍스처 사용률 {surface.Coverage * 100f:0.0}%", this);
        if (surface.Coverage < 0.2f)
            Debug.LogWarning("[BodyDraw] 텍스처 사용률이 낮습니다. 모델 임포트 설정 Lightmap UV의 Margin Method를 Manual로 해 주세요.", this);

        texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            name = "BodyPaintTexture",
            wrapMode = TextureWrapMode.Clamp,
        };
        skinColor = BodyPaintSurface.SetupBodyMaterials(targetRenderer, faceMaterialIndex, bodyShader, texture);

        paint = BodyPaintStorage.LoadApplied(textureSize); // 지금 적용된 그림으로 시작
        display = new Color32[paint.Length];
        dirty = surface.FullRect;

        var go = new GameObject("BodyDrawCollider") { layer = 2, hideFlags = HideFlags.DontSave };
        go.transform.SetParent(targetRenderer.transform, false);
        meshCollider = go.AddComponent<MeshCollider>();

        SetCamera(false);
    }

    private void OnDestroy()
    {
        if (colliderMesh != null) Destroy(colliderMesh);
        if (meshCollider != null) Destroy(meshCollider.gameObject);
        if (texture != null) Destroy(texture);
    }

    /// <summary>그리기 카메라 우선순위를 전체 보기보다 높이거나 낮춘다</summary>
    private void SetCamera(bool draw)
    {
        int basePriority = overviewCamera.Priority.Value;
        drawCamera.Priority = draw ? basePriority + 1 : basePriority - 1;
    }

    private void BakeCollider()
    {
        if (colliderMesh == null)
            colliderMesh = new Mesh { name = "BodyDrawBaked", hideFlags = HideFlags.DontSave };
        targetRenderer.BakeMesh(colliderMesh); // 렌더러 로컬 공간 → 콜라이더가 자식이라 회전도 따라간다
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = colliderMesh;
        surface.UpdatePose(colliderMesh);      // 픽셀별 3D 위치도 같은 포즈로
    }

    // ───────── 입력 ─────────

    private void Update()
    {
        if (!IsDrawing || brain.IsBlending)
            return;

        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        // 오른쪽 드래그: 캐릭터 회전 (뒤/옆에도 그릴 수 있게)
        if (mouse.rightButton.isPressed)
            transform.Rotate(0f, -mouse.delta.ReadValue().x * rotateSpeed, 0f, Space.World);

        if (!mouse.leftButton.isPressed)
        {
            pressing = false;
            return;
        }

        Vector2 mousePos = mouse.position.ReadValue();
        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return; // UI 위에서 누른 건 그림 아님
            pressing = true;
            undoSaved = false;
            hasLastPoint = false;
        }

        if (!pressing)
            return;

        Ray ray = outputCamera.ScreenPointToRay(mousePos);
        if (meshCollider.Raycast(ray, out RaycastHit hit, 1000f))
        {
            // 맞은 지점을 콜라이더(=구운 메시) 로컬 좌표로. 픽셀 위치도 같은 공간이라 바로 비교된다.
            Transform local = meshCollider.transform;
            Vector3 point = local.InverseTransformPoint(hit.point);
            Vector3 normal = local.InverseTransformDirection(hit.normal).normalized;
            if (!undoSaved)
            {
                PushUndo(); // 실제로 칠할 때 획마다 한 번만
                undoSaved = true;
            }
            PaintTo(point, normal);
        }
        else
        {
            hasLastPoint = false; // 허공으로 나갔다 오면 선을 잇지 않는다
        }
    }

    private float BrushRadius => brushSize * 0.002f * surface.Height;

    private void PaintTo(Vector3 point, Vector3 normal)
    {
        float radius = BrushRadius;
        Color32 color = brushColor;
        color.a = 255;

        if (hasLastPoint)
        {
            // 빠르게 움직여도 끊기지 않게 직전 위치부터 촘촘히
            int count = Mathf.CeilToInt(Vector3.Distance(lastPoint, point) / Mathf.Max(radius * 0.3f, 1e-5f));
            for (int i = 1; i <= count; i++)
                surface.Stamp(paint, Vector3.Lerp(lastPoint, point, i / (float)count), normal, radius, color, eraser, ref dirty);
        }
        else
        {
            surface.Stamp(paint, point, normal, radius, color, eraser, ref dirty);
        }

        lastPoint = point;
        hasLastPoint = true;
    }

    private void LateUpdate()
    {
        // 바뀐 영역만 합성하고 GPU 업로드는 프레임당 한 번
        if (dirty.width <= 0 || !IsReady)
            return;

        surface.Composite(paint, skinColor, display, dirty);
        texture.SetPixels32(display);
        texture.Apply(false);
        dirty = default;
    }

    private void PushUndo()
    {
        undoStack.Add((Color32[])paint.Clone());
        if (undoStack.Count > maxUndo)
            undoStack.RemoveAt(0);
    }

    // ───────── UI에서 호출 ─────────

    public void BeginDrawing()
    {
        if (IsDrawing || !IsReady)
            return;

        startRotation = transform.rotation;
        BakeCollider();
        SetCamera(true);
        IsDrawing = true;
        onDrawingStarted?.Invoke();
    }

    /// <summary>이름 붙여 파일로 저장하고 몸에 적용한 뒤 그리기 종료. 이름이 비었으면 false.</summary>
    public bool Confirm(string name)
    {
        name = BodyPaintStorage.CleanName(name);
        if (!IsDrawing || name == null)
            return false;

        BodyPaintStorage.Save(name, BodyPaintStorage.Encode(paint, textureSize));
        BodyPaintStorage.Applied = name;

        // 에디터에서는 피부색까지 합친 최종 이미지로 머티리얼 에셋도 만든다
        surface.Composite(paint, skinColor, display, surface.FullRect);
        BodyPaintStorage.SaveMaterialAsset(name, BodyPaintStorage.Encode(display, textureSize), bodyShader);
        undoStack.Clear(); // 확정한 그림이 새 시작점
        EndDrawing();
        return true;
    }

    /// <summary>저장 없이 종료. 마지막으로 확정한 그림으로 되돌린다.</summary>
    public void Cancel()
    {
        if (!IsDrawing)
            return;

        paint = BodyPaintStorage.LoadApplied(textureSize);
        undoStack.Clear();
        dirty = surface.FullRect;
        EndDrawing();
    }

    private void EndDrawing()
    {
        pressing = false;
        transform.rotation = startRotation;
        SetCamera(false);
        IsDrawing = false;
        onDrawingEnded?.Invoke();
    }

    public void Undo()
    {
        if (undoStack.Count == 0)
            return;
        paint = undoStack[undoStack.Count - 1];
        undoStack.RemoveAt(undoStack.Count - 1);
        dirty = surface.FullRect;
    }

    public void Clear()
    {
        PushUndo();
        paint = new Color32[paint.Length];
        dirty = surface.FullRect;
    }

    /// <summary>저장된 그림을 캔버스에 불러오기 (확정해야 적용됨)</summary>
    public bool LoadDesign(string name)
    {
        Color32[] src = BodyPaintStorage.Decode(BodyPaintStorage.Load(name), textureSize);
        if (src == null)
            return false;

        PushUndo();
        paint = src;
        dirty = surface.FullRect;
        return true;
    }

    public void DeleteDesign(string name) => BodyPaintStorage.Delete(name);

    public void SetColor(Color color) { brushColor = color; eraser = false; }
    public void SetBrushSize(float size) => brushSize = size;
    public void SetEraser(bool on) => eraser = on;
}
