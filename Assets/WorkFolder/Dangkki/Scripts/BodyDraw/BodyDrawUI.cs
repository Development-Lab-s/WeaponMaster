using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 몸 그리기 UI.
/// 평소: [몸 그리기] 버튼. 그리기 중: 색/브러시/펜·지우개/되돌리기·전체 지우기 + 이름 + 확정/취소 + 저장된 그림 목록(불러오기/삭제)
/// 씬은 메뉴 Tools > BodyDraw > 새 씬 만들기 로 생성한다.
/// </summary>
public class BodyDrawUI : MonoBehaviour
{
    [SerializeField] private BodyDraw bodyDraw;
    [SerializeField] private Button startButton;
    [SerializeField] private GameObject drawPanel;

    [Header("색 / 브러시")]
    [SerializeField] private FaceColorPicker colorPicker;
    [SerializeField] private Slider brushSlider;
    [SerializeField] private TMP_Text brushLabel;

    [Header("버튼")]
    [SerializeField] private Button penButton;
    [SerializeField] private Button eraserButton;
    [SerializeField] private Button undoButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button confirmButton;

    [Header("저장 / 목록")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Transform listContent;
    [Tooltip("꺼 둔 목록 항목 원본. 자식: LoadButton(글자 포함), DeleteButton")]
    [SerializeField] private GameObject listItemTemplate;

    [Header("색")]
    [SerializeField] private Color selectedColor = new Color(0.9f, 0.5f, 0.09f);
    [SerializeField] private Color normalColor = new Color(0.22f, 0.22f, 0.25f);

    private void Awake()
    {
        bodyDraw.onDrawingStarted.AddListener(() => SetDrawing(true));
        bodyDraw.onDrawingEnded.AddListener(() => SetDrawing(false));

        startButton.onClick.AddListener(bodyDraw.BeginDrawing);
        undoButton.onClick.AddListener(bodyDraw.Undo);
        clearButton.onClick.AddListener(bodyDraw.Clear);
        cancelButton.onClick.AddListener(bodyDraw.Cancel);
        confirmButton.onClick.AddListener(Confirm);
        penButton.onClick.AddListener(() => SetEraser(false));
        eraserButton.onClick.AddListener(() => SetEraser(true));

        colorPicker.onColorChanged.AddListener(color =>
        {
            bodyDraw.SetColor(color);
            RefreshToolButtons();
        });

        brushSlider.minValue = 1f;
        brushSlider.maxValue = 64f;
        brushSlider.onValueChanged.AddListener(size =>
        {
            bodyDraw.SetBrushSize(size);
            brushLabel.text = $"브러시 크기  {size:0}";
        });

        listItemTemplate.SetActive(false);
        SetDrawing(false);
    }

    private void Start()
    {
        colorPicker.SetColor(bodyDraw.brushColor);
        brushSlider.value = bodyDraw.brushSize;
        brushLabel.text = $"브러시 크기  {bodyDraw.brushSize:0}";
        RefreshToolButtons();
    }

    private void Update()
    {
        if (drawPanel.activeSelf)
            undoButton.interactable = bodyDraw.CanUndo;
    }

    private void SetDrawing(bool drawing)
    {
        drawPanel.SetActive(drawing);
        startButton.gameObject.SetActive(!drawing);

        if (drawing)
        {
            nameInput.text = BodyPaintStorage.Applied ?? "";
            statusText.text = "오른쪽 드래그: 캐릭터 돌리기";
            RefreshList();
        }
    }

    private void Confirm()
    {
        if (!bodyDraw.Confirm(nameInput.text))
            statusText.text = "이름을 입력해 주세요";
    }

    // ───────── 저장된 그림 목록 ─────────

    private void RefreshList()
    {
        // 원본 템플릿만 남기고 지우기
        for (int i = listContent.childCount - 1; i >= 0; i--)
        {
            GameObject child = listContent.GetChild(i).gameObject;
            if (child != listItemTemplate)
                Destroy(child);
        }

        string applied = BodyPaintStorage.Applied;
        foreach (string name in BodyPaintStorage.List())
        {
            GameObject item = Instantiate(listItemTemplate, listContent);
            item.SetActive(true);

            Button load = item.transform.Find("LoadButton").GetComponent<Button>();
            Button delete = item.transform.Find("DeleteButton").GetComponent<Button>();
            load.GetComponentInChildren<TMP_Text>().text = name == applied ? $"{name}  <color=#FFC870>(적용중)</color>" : name;

            string captured = name;
            load.onClick.AddListener(() =>
            {
                if (bodyDraw.LoadDesign(captured))
                {
                    nameInput.text = captured;
                    statusText.text = $"'{captured}' 불러옴 (확정하면 적용)";
                }
            });
            delete.onClick.AddListener(() =>
            {
                bodyDraw.DeleteDesign(captured);
                statusText.text = $"'{captured}' 삭제됨";
                RefreshList();
            });
        }
    }

    // ───────── 도구 ─────────

    private void SetEraser(bool on)
    {
        bodyDraw.SetEraser(on);
        RefreshToolButtons();
    }

    private void RefreshToolButtons()
    {
        penButton.image.color = bodyDraw.eraser ? normalColor : selectedColor;
        eraserButton.image.color = bodyDraw.eraser ? selectedColor : normalColor;
    }
}
