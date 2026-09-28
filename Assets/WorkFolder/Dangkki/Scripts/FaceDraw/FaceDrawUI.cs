using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FaceDrawUI : MonoBehaviour
{
    [Header("나머지")]
    [SerializeField] private FaceDraw faceDraw;
    [SerializeField] private GameObject drawPanel;
    [SerializeField] private Button startButton;
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
    
    [Header("색")]
    [SerializeField]private Color selectedColor = new Color(0.9f, 0.5f, 0.09f);
    [SerializeField] private Color normalColor = new Color(0.22f, 0.22f, 0.25f);

    private void Awake()
    {
        faceDraw.onDrawingStarted.AddListener(() => SetDrawing(true));
        faceDraw.onDrawingEnded.AddListener(() => SetDrawing(false));

        startButton.onClick.AddListener(faceDraw.BeginDrawing);
        undoButton.onClick.AddListener(faceDraw.Undo);
        clearButton.onClick.AddListener(faceDraw.Clear);
        cancelButton.onClick.AddListener(faceDraw.Cancel);
        confirmButton.onClick.AddListener(faceDraw.Confirm);
        penButton.onClick.AddListener(() => SetEraser(false));
        eraserButton.onClick.AddListener(() => SetEraser(true));

        colorPicker.onColorChanged.AddListener(color =>
        {
            faceDraw.SetColor(color);
            RefreshToolButtons();
        });

        brushSlider.minValue = 1f;
        brushSlider.maxValue = 64f;
        brushSlider.onValueChanged.AddListener(radius =>
        {
            faceDraw.SetBrushRadius(radius);
            brushLabel.text = $"브러시 크기  {radius:0}";
        });

        SetDrawing(false);
    }

    private void Start()
    {
        colorPicker.SetColor(faceDraw.brushColor);
        brushSlider.value = faceDraw.brushRadius;
        brushLabel.text = $"브러시 크기  {faceDraw.brushRadius:0}";
        RefreshToolButtons();
    }

    private void Update()
    {
        if (drawPanel.activeSelf)
            undoButton.interactable = faceDraw.CanUndo && !faceDraw.IsSaving;
        confirmButton.interactable = !faceDraw.InputLocked && !faceDraw.IsSaving;
        cancelButton.interactable = !faceDraw.IsSaving;
        clearButton.interactable = !faceDraw.InputLocked && !faceDraw.IsSaving;
        startButton.interactable = !faceDraw.InputLocked && !faceDraw.IsSaving;
    }

    private void SetDrawing(bool drawing)
    {
        drawPanel.SetActive(drawing);
        startButton.gameObject.SetActive(!drawing && !faceDraw.SkipStartButton);
    }

    private void SetEraser(bool on)
    {
        faceDraw.SetEraser(on);
        RefreshToolButtons();
    }

    private void RefreshToolButtons()
    {
        penButton.image.color = faceDraw.eraser ? normalColor : selectedColor;
        eraserButton.image.color = faceDraw.eraser ? selectedColor : normalColor;
    }
}
