using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class FaceColorPicker : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    private const int SvTexSize = 64;

    [SerializeField] private RawImage svArea;
    [SerializeField] private RectTransform svHandle;
    [SerializeField] private Image svHandleFill;
    [SerializeField] private RawImage hueBar;
    [SerializeField] private RectTransform hueHandle;
    [SerializeField] private Image hueHandleFill;
    [SerializeField] private Image preview;

    public UnityEvent<Color> onColorChanged;

    private float hue;
    private float sat;
    private float value;

    private Texture2D svTex;
    private Texture2D hueTex;
    private RectTransform dragging;

    public Color Color => Color.HSVToRGB(hue, sat, value);

    private void Awake()
    {
        hueTex = new Texture2D(256, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int x = 0; x < 256; x++)
            hueTex.SetPixel(x, 0, Color.HSVToRGB(x / 255f, 1f, 1f));
        hueTex.Apply(false);
        hueBar.texture = hueTex;

        svTex = new Texture2D(SvTexSize, SvTexSize, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        svArea.texture = svTex;
        RefreshSvTexture();
        RefreshHandles();
    }
    
    
    public void SetColor(Color color)
    {
        Color.RGBToHSV(color, out float h, out sat, out value);
        if (sat > 0f && value > 0f)
            hue = h;
        RefreshSvTexture();
        RefreshHandles();
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (RectTransformUtility.RectangleContainsScreenPoint(svArea.rectTransform, e.position, e.pressEventCamera))
            dragging = svArea.rectTransform;
        else if (RectTransformUtility.RectangleContainsScreenPoint(hueBar.rectTransform, e.position, e.pressEventCamera))
            dragging = hueBar.rectTransform;
        else
            dragging = null;

        Pick(e);
    }

    public void OnDrag(PointerEventData e)
    {
        Pick(e);
    }



    private void Pick(PointerEventData e)
    {
        if (dragging == null)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(dragging, e.position, e.pressEventCamera, out Vector2 local);
        Vector2 n = Rect.PointToNormalized(dragging.rect, local);

        if (dragging == svArea.rectTransform)
        {
            sat = n.x;
            value = n.y;
        }
        else
        {
            hue = Mathf.Min(n.x, 0.9999f);
            RefreshSvTexture();
        }

        RefreshHandles();
        onColorChanged?.Invoke(Color);
    }
    
    private void RefreshSvTexture()
    {
        if (svTex == null)
            return;

        var pixels = new Color32[SvTexSize * SvTexSize];
        float max = SvTexSize - 1;
        for (int y = 0; y < SvTexSize; y++)
            for (int x = 0; x < SvTexSize; x++)
                pixels[y * SvTexSize + x] = Color.HSVToRGB(hue, x / max, y / max);
        svTex.SetPixels32(pixels);
        svTex.Apply(false);
    }

    private void RefreshHandles()
    {
        SetAnchor(svHandle, new Vector2(sat, value));
        SetAnchor(hueHandle, new Vector2(hue, 0.5f));

        Color c = Color;
        svHandleFill.color = c;
        hueHandleFill.color = Color.HSVToRGB(hue, 1f, 1f);
        preview.color = c;
    }

    private static void SetAnchor(RectTransform rect, Vector2 anchor)
    {
        if (rect == null)
            return;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = Vector2.zero;
    }
}
