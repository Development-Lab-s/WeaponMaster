using UnityEngine;

/// <summary>
/// 다른 씬의 캐릭터에 붙이면, BodyDraw에서 확정(적용)해 둔 몸 그림 파일을 불러와 몸에 입힌다.
/// BodyDraw 씬과 같은 모델(같은 라이트맵 UV)이어야 그림 위치가 맞는다.
/// </summary>
public class BodyPaintApplier : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer targetRenderer;
    [SerializeField] private int faceMaterialIndex = 1;
    [SerializeField] private Shader bodyShader;
    [Tooltip("BodyDraw의 textureSize와 같아야 한다")]
    [SerializeField] private int textureSize = 1024;
    [SerializeField] private bool applyOnStart = true;

    private Texture2D texture;
    private Color32 skin;

    private void Start()
    {
        if (applyOnStart)
            Apply();
    }

    private void OnDestroy()
    {
        if (texture != null)
            Destroy(texture);
    }

    /// <summary>적용된 그림으로 다시 입힌다 (BodyDraw에서 바꾼 뒤 호출하면 갱신)</summary>
    public void Apply()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

        BodyPaintSurface surface;
        try
        {
            surface = new BodyPaintSurface(targetRenderer, faceMaterialIndex, textureSize);
        }
        catch (System.InvalidOperationException e)
        {
            Debug.LogError("[BodyPaintApplier] " + e.Message, this);
            return;
        }

        if (texture == null)
        {
            // 머티리얼은 처음 한 번만 교체 (두 번째부터는 색이 이미 흰색이라 피부색을 다시 못 읽음)
            texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false) { name = "BodyPaintTexture", wrapMode = TextureWrapMode.Clamp };
            skin = BodyPaintSurface.SetupBodyMaterials(targetRenderer, faceMaterialIndex, bodyShader, texture);
        }

        Color32[] paint = BodyPaintStorage.LoadApplied(textureSize);
        var display = new Color32[paint.Length];
        surface.Composite(paint, skin, display, surface.FullRect);
        texture.SetPixels32(display);
        texture.Apply(false);
    }
}
