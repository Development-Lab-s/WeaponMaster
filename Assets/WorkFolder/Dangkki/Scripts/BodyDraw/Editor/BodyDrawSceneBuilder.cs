using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 메뉴 Tools > BodyDraw > 새 씬 만들기
/// 캐릭터, 카메라(메인 + 시네머신 전체 보기/그리기), 조명, UI, EventSystem을 갖춘 몸 그리기 씬을 만들어 저장한다.
/// 시네머신 카메라는 몸 크기에 맞춰 자동 배치되니, 만든 뒤 원하는 구도로 옮기면 된다.
/// </summary>
public static class BodyDrawSceneBuilder
{
    private const string ScenePath = "Assets/WorkFolder/Dangkki/Scenes/BodyDraw.unity";
    private const string ModelPath = "Assets/WorkFolder/Dangkki/character_re.fbx";
    private const string FontPath = "Assets/WorkFolder/Dangkki/Fonts/MALGUN SDF.asset";
    private const float CameraFov = 40f;

    private static readonly Color PanelColor = new Color(0.08f, 0.08f, 0.1f, 0.88f);
    private static readonly Color ButtonColor = new Color(0.22f, 0.22f, 0.25f);
    private static readonly Color SaveColor = new Color(0.2f, 0.65f, 0.35f);
    private static readonly Color DeleteColor = new Color(0.55f, 0.22f, 0.22f);

    private static Sprite roundSprite;
    private static Sprite knobSprite;
    private static TMP_FontAsset font;

    [MenuItem("Tools/BodyDraw/새 씬 만들기")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        if (System.IO.File.Exists(ScenePath) &&
            !EditorUtility.DisplayDialog("BodyDraw", $"{ScenePath} 가 이미 있습니다. 새로 만들까요?", "덮어쓰기", "취소"))
            return;

        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null)
        {
            EditorUtility.DisplayDialog("BodyDraw", $"모델을 찾을 수 없습니다: {ModelPath}", "확인");
            return;
        }
        if (!EnsureLightmapUV())
            return;

        roundSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        // 기본 씬 (Main Camera + Directional Light + 스카이박스/환경광)
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var light = Object.FindAnyObjectByType<Light>();
        light.shadows = LightShadows.Soft;

        // ── 캐릭터 ──
        var character = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
        character.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        var renderer = character.GetComponentInChildren<SkinnedMeshRenderer>();
        int faceIndex = FindFaceIndex(renderer);

        // ── 메인 카메라 (실제 화면) + 브레인 ──
        Camera cam = Camera.main;
        GameObject camGo = cam.gameObject;
        cam.nearClipPlane = 0.05f;
        var brain = camGo.AddComponent<CinemachineBrain>();
        brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.6f);

        // ── 시네머신 카메라: 전체 보기 + 그리기 ──
        ComputeFraming(renderer, faceIndex, out Bounds bodyBounds, out Vector3 forward);
        var camRoot = new GameObject("Cameras").transform;

        CinemachineCamera overview = CreateVirtualCamera("CM Overview", camRoot, bodyBounds, forward, 1.15f, 0f);
        overview.Priority = 10;
        // 그리기는 오른쪽 패널을 피해 캐릭터가 화면 왼쪽에 보이게
        CinemachineCamera drawCam = CreateVirtualCamera("CM Draw", camRoot, bodyBounds, forward, 1.05f, 0.3f);
        drawCam.Priority = 0;
        camGo.transform.SetPositionAndRotation(overview.transform.position, overview.transform.rotation);

        // ── BodyDraw ──
        var bodyDraw = character.AddComponent<BodyDraw>();
        var drawSo = new SerializedObject(bodyDraw);
        drawSo.FindProperty("targetRenderer").objectReferenceValue = renderer;
        drawSo.FindProperty("faceMaterialIndex").intValue = faceIndex;
        drawSo.FindProperty("bodyShader").objectReferenceValue = Shader.Find("Dangkki/BodyPaintLit");
        drawSo.FindProperty("overviewCamera").objectReferenceValue = overview;
        drawSo.FindProperty("drawCamera").objectReferenceValue = drawCam;
        drawSo.ApplyModifiedPropertiesWithoutUndo();

        // ── UI ──
        BuildUI(bodyDraw);
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        EditorSceneManager.SaveScene(scene, ScenePath);
        Selection.activeGameObject = character;
        Debug.Log($"[BodyDraw] 씬 생성 완료: {ScenePath}");
    }

    // ───────── 준비 ─────────

    /// <summary>몸 그리기는 라이트맵 UV(uv2)를 쓰므로 FBX 임포트 설정에서 켜져 있어야 한다</summary>
    private static bool EnsureLightmapUV()
    {
        // Margin Method가 Calculate면 저해상도 라이트맵 기준 여백을 잡아서 UV 조각이 아주 작아진다 → 1024 기준 수동 4px
        var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        if (importer.generateSecondaryUV && importer.secondaryUVMarginMethod == ModelImporterSecondaryUVMarginMethod.Manual)
            return true;

        if (!EditorUtility.DisplayDialog("BodyDraw", "모델의 Generate Lightmap UVs 설정을 바꿔야 합니다 (켜기 + 여백 수동 4px). 바꾸고 다시 임포트할까요?", "바꾸기", "취소"))
            return false;
        importer.generateSecondaryUV = true;
        importer.secondaryUVMarginMethod = ModelImporterSecondaryUVMarginMethod.Manual;
        importer.secondaryUVPackMargin = 4;
        importer.SaveAndReimport();
        return true;
    }

    private static int FindFaceIndex(SkinnedMeshRenderer renderer)
    {
        Material[] mats = renderer.sharedMaterials;
        for (int i = 0; i < mats.Length; i++)
            if (mats[i] != null && mats[i].name.Contains("Face"))
                return i;
        return 1;
    }

    /// <summary>현재 포즈 기준 몸 전체 월드 범위와 캐릭터 정면 방향(얼굴 법선 평균)</summary>
    private static void ComputeFraming(SkinnedMeshRenderer renderer, int faceIndex, out Bounds body, out Vector3 forward)
    {
        var baked = new Mesh();
        renderer.BakeMesh(baked);
        Vector3[] verts = baked.vertices;
        Vector3[] normals = baked.normals;
        Transform t = renderer.transform;

        body = new Bounds(t.TransformPoint(verts[0]), Vector3.zero);
        foreach (Vector3 v in verts)
            body.Encapsulate(t.TransformPoint(v));

        Vector3 faceNormal = Vector3.zero;
        foreach (int i in renderer.sharedMesh.GetIndices(faceIndex))
            faceNormal += t.TransformDirection(normals[i]);
        Object.DestroyImmediate(baked);

        // 얼굴이 향한 방향 = 캐릭터 정면
        forward = Vector3.ProjectOnPlane(faceNormal, Vector3.up);
        forward = forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.back;
    }

    /// <param name="shift">대상이 화면에서 옆으로 비켜 보이게 하는 양 (+ = 왼쪽에 보임)</param>
    private static CinemachineCamera CreateVirtualCamera(string name, Transform parent, Bounds target, Vector3 forward,
                                                         float padding, float shift)
    {
        // 몸 높이가 화면 세로에 맞게
        float halfFov = CameraFov * 0.5f * Mathf.Deg2Rad;
        float dist = target.extents.y * padding / Mathf.Tan(halfFov);

        Quaternion rot = Quaternion.LookRotation(-forward, Vector3.up);
        float halfWidth = dist * Mathf.Tan(halfFov) * (16f / 9f);
        Vector3 pos = target.center + forward * dist + rot * Vector3.right * (halfWidth * shift);

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(pos, rot);
        var vcam = go.AddComponent<CinemachineCamera>();
        LensSettings lens = vcam.Lens;
        lens.FieldOfView = CameraFov;
        lens.NearClipPlane = 0.05f;
        vcam.Lens = lens;
        return vcam;
    }

    // ───────── UI ─────────

    private static void BuildUI(BodyDraw bodyDraw)
    {
        var root = new GameObject("BodyDrawUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)) { layer = 5 };
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // ── 평소: 아래 가운데 [몸 그리기] 버튼 ──
        Button start = CreateButton(root.transform, "StartButton", "몸 그리기", ButtonColor, 30);
        var startRect = (RectTransform)start.transform;
        startRect.anchorMin = startRect.anchorMax = new Vector2(0.5f, 0f);
        startRect.pivot = new Vector2(0.5f, 0f);
        startRect.sizeDelta = new Vector2(300, 80);
        startRect.anchoredPosition = new Vector2(0, 60);

        // ── 그리기: 오른쪽 패널 ──
        GameObject draw = CreatePanel(root.transform, "DrawPanel", 420, new Vector2(1f, 0.5f), -40);

        TMP_Text title = CreateText(draw.transform, "Title", "몸 그리기", 32, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        SetHeight(title.gameObject, 46);

        FaceColorPicker picker = CreateColorPicker(draw.transform, 260, 26, out Image preview);

        GameObject brushRow = CreateRow(draw.transform, "BrushRow", 56);
        brushRow.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
        preview.transform.SetParent(brushRow.transform, false);
        preview.gameObject.AddComponent<LayoutElement>().preferredWidth = 56;
        GameObject brushColumn = CreateUI("Brush", brushRow.transform);
        brushColumn.AddComponent<LayoutElement>().flexibleWidth = 1;
        var column = brushColumn.AddComponent<VerticalLayoutGroup>();
        column.spacing = 4;
        column.childControlWidth = column.childControlHeight = true;
        column.childForceExpandWidth = true;
        column.childForceExpandHeight = false;
        TMP_Text brushLabel = CreateText(brushColumn.transform, "Label", "브러시 크기", 22, TextAlignmentOptions.Left);
        SetHeight(brushLabel.gameObject, 26);
        Slider slider = CreateSlider(brushColumn.transform);

        GameObject toolRow = CreateRow(draw.transform, "ToolRow", 48);
        Button pen = CreateButton(toolRow.transform, "PenButton", "펜", ButtonColor, 22);
        Button eraser = CreateButton(toolRow.transform, "EraserButton", "지우개", ButtonColor, 22);

        GameObject editRow = CreateRow(draw.transform, "EditRow", 48);
        Button undo = CreateButton(editRow.transform, "UndoButton", "되돌리기", ButtonColor, 22);
        Button clear = CreateButton(editRow.transform, "ClearButton", "전체 지우기", ButtonColor, 22);

        TMP_InputField input = CreateInputField(draw.transform);
        SetHeight(input.gameObject, 48);

        GameObject finishRow = CreateRow(draw.transform, "FinishRow", 60);
        Button cancel = CreateButton(finishRow.transform, "CancelButton", "취소", DeleteColor, 26);
        Button confirm = CreateButton(finishRow.transform, "ConfirmButton", "확정", SaveColor, 26);

        TMP_Text status = CreateText(draw.transform, "Status", "", 20, TextAlignmentOptions.Left);
        status.color = new Color(1f, 0.85f, 0.5f);
        SetHeight(status.gameObject, 24);

        TMP_Text listTitle = CreateText(draw.transform, "ListTitle", "저장된 그림 (누르면 불러오기)", 20, TextAlignmentOptions.Left);
        SetHeight(listTitle.gameObject, 24);
        Transform listContent = CreateScrollList(draw.transform, 180, out GameObject template);

        // ── 연결 ──
        var ui = root.AddComponent<BodyDrawUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("bodyDraw").objectReferenceValue = bodyDraw;
        so.FindProperty("startButton").objectReferenceValue = start;
        so.FindProperty("drawPanel").objectReferenceValue = draw;
        so.FindProperty("colorPicker").objectReferenceValue = picker;
        so.FindProperty("brushSlider").objectReferenceValue = slider;
        so.FindProperty("brushLabel").objectReferenceValue = brushLabel;
        so.FindProperty("penButton").objectReferenceValue = pen;
        so.FindProperty("eraserButton").objectReferenceValue = eraser;
        so.FindProperty("undoButton").objectReferenceValue = undo;
        so.FindProperty("clearButton").objectReferenceValue = clear;
        so.FindProperty("cancelButton").objectReferenceValue = cancel;
        so.FindProperty("confirmButton").objectReferenceValue = confirm;
        so.FindProperty("nameInput").objectReferenceValue = input;
        so.FindProperty("statusText").objectReferenceValue = status;
        so.FindProperty("listContent").objectReferenceValue = listContent;
        so.FindProperty("listItemTemplate").objectReferenceValue = template;
        so.ApplyModifiedPropertiesWithoutUndo();

        draw.SetActive(false);
    }

    private static GameObject CreatePanel(Transform parent, string name, float width, Vector2 anchor, float x)
    {
        GameObject panel = CreateUI(name, parent);
        AddImage(panel, roundSprite, PanelColor);
        var rect = (RectTransform)panel.transform;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.sizeDelta = new Vector2(width, 0);
        rect.anchoredPosition = new Vector2(x, 0);

        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.spacing = 10;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        panel.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return panel;
    }

    private static FaceColorPicker CreateColorPicker(Transform parent, float svHeight, float hueHeight, out Image preview)
    {
        const float gap = 12f;
        GameObject go = CreateUI("ColorPicker", parent);
        SetHeight(go, svHeight + gap + hueHeight);

        GameObject sv = CreateUI("SVArea", go.transform);
        sv.AddComponent<RawImage>();
        var svRect = (RectTransform)sv.transform;
        svRect.anchorMin = new Vector2(0, 1);
        svRect.anchorMax = new Vector2(1, 1);
        svRect.pivot = new Vector2(0.5f, 1);
        svRect.sizeDelta = new Vector2(0, svHeight);
        RectTransform svHandle = CreateHandle(sv.transform, "SVHandle", 32, out Image svFill);

        GameObject hue = CreateUI("HueBar", go.transform);
        hue.AddComponent<RawImage>();
        var hueRect = (RectTransform)hue.transform;
        hueRect.anchorMin = new Vector2(0, 0);
        hueRect.anchorMax = new Vector2(1, 0);
        hueRect.pivot = new Vector2(0.5f, 0);
        hueRect.sizeDelta = new Vector2(0, hueHeight);
        RectTransform hueHandle = CreateHandle(hue.transform, "HueHandle", 34, out Image hueFill);

        GameObject previewGo = CreateUI("ColorPreview", parent);
        preview = AddImage(previewGo, roundSprite, Color.black);
        preview.raycastTarget = false;

        var picker = go.AddComponent<FaceColorPicker>();
        var so = new SerializedObject(picker);
        so.FindProperty("svArea").objectReferenceValue = sv.GetComponent<RawImage>();
        so.FindProperty("svHandle").objectReferenceValue = svHandle;
        so.FindProperty("svHandleFill").objectReferenceValue = svFill;
        so.FindProperty("hueBar").objectReferenceValue = hue.GetComponent<RawImage>();
        so.FindProperty("hueHandle").objectReferenceValue = hueHandle;
        so.FindProperty("hueHandleFill").objectReferenceValue = hueFill;
        so.FindProperty("preview").objectReferenceValue = preview;
        so.ApplyModifiedPropertiesWithoutUndo();
        return picker;
    }

    private static RectTransform CreateHandle(Transform parent, string name, float size, out Image fill)
    {
        GameObject ring = CreateUI(name, parent);
        AddImage(ring, knobSprite, Color.white).raycastTarget = false;
        var shadow = ring.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.5f);
        shadow.effectDistance = new Vector2(1, -2);
        ((RectTransform)ring.transform).sizeDelta = new Vector2(size, size);

        GameObject inner = CreateUI("Fill", ring.transform);
        fill = AddImage(inner, knobSprite, Color.red);
        fill.raycastTarget = false;
        var innerRect = (RectTransform)inner.transform;
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.sizeDelta = new Vector2(-8, -8);
        return (RectTransform)ring.transform;
    }

    /// <summary>스크롤 목록 + 꺼 둔 항목 원본(LoadButton + DeleteButton)</summary>
    private static Transform CreateScrollList(Transform parent, float height, out GameObject template)
    {
        var resources = new DefaultControls.Resources
        {
            standard = roundSprite,
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            knob = knobSprite,
        };
        GameObject scroll = DefaultControls.CreateScrollView(resources);
        scroll.name = "SavedList";
        SetLayerRecursive(scroll);
        scroll.transform.SetParent(parent, false);
        SetHeight(scroll, height);
        scroll.GetComponent<Image>().color = new Color(0, 0, 0, 0.35f);

        var scrollRect = scroll.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        Object.DestroyImmediate(scrollRect.horizontalScrollbar.gameObject);
        scrollRect.horizontalScrollbar = null;

        RectTransform content = scrollRect.content;
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.spacing = 6;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        template = CreateRow(content, "ItemTemplate", 44);
        template.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
        Button load = CreateButton(template.transform, "LoadButton", "이름", ButtonColor, 20);
        load.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        load.GetComponentInChildren<TMP_Text>().alignment = TextAlignmentOptions.Left;
        load.GetComponentInChildren<TMP_Text>().margin = new Vector4(12, 0, 0, 0);
        Button delete = CreateButton(template.transform, "DeleteButton", "삭제", DeleteColor, 20);
        delete.gameObject.AddComponent<LayoutElement>().preferredWidth = 70;
        template.SetActive(false);

        return content;
    }

    private static TMP_InputField CreateInputField(Transform parent)
    {
        var resources = new TMP_DefaultControls.Resources
        {
            standard = roundSprite,
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
        };
        GameObject go = TMP_DefaultControls.CreateInputField(resources);
        go.name = "NameInput";
        SetLayerRecursive(go);
        go.transform.SetParent(parent, false);

        var input = go.GetComponent<TMP_InputField>();
        if (font != null)
            input.fontAsset = font;
        input.pointSize = 22;
        input.characterLimit = 30;
        ((TMP_Text)input.placeholder).text = "그림 이름";
        return input;
    }

    private static Slider CreateSlider(Transform parent)
    {
        var resources = new DefaultControls.Resources
        {
            standard = roundSprite,
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            knob = knobSprite,
        };
        GameObject go = DefaultControls.CreateSlider(resources);
        go.name = "BrushSlider";
        SetLayerRecursive(go);
        go.transform.SetParent(parent, false);
        SetHeight(go, 22);
        return go.GetComponent<Slider>();
    }

    // ───────── 공통 ─────────

    private static GameObject CreateUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void SetLayerRecursive(GameObject go)
    {
        foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = 5;
    }

    private static Image AddImage(GameObject go, Sprite sprite, Color color)
    {
        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        return image;
    }

    private static void SetHeight(GameObject go, float height)
    {
        var element = go.GetComponent<LayoutElement>();
        if (element == null)
            element = go.AddComponent<LayoutElement>();
        element.preferredHeight = height;
        element.minHeight = height;
    }

    private static GameObject CreateRow(Transform parent, string name, float height)
    {
        GameObject row = CreateUI(name, parent);
        SetHeight(row, height);
        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        return row;
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, float size, TextAlignmentOptions align)
    {
        GameObject go = CreateUI(name, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null)
            tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button CreateButton(Transform parent, string name, string label, Color color, float fontSize)
    {
        GameObject go = CreateUI(name, parent);
        Image image = AddImage(go, roundSprite, color);
        var button = go.AddComponent<Button>();
        button.targetGraphic = image;

        TMP_Text text = CreateText(go.transform, "Text", label, fontSize, TextAlignmentOptions.Center);
        var rect = (RectTransform)text.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        return button;
    }
}
