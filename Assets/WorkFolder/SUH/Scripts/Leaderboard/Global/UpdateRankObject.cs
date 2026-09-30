using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkFolder.SUH.Scripts.Leaderboard.Global
{
    public class UpdateRankObject : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI killText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private Image profileImage;

        private Image faceOverlay;
        private Texture2D faceTexture;
        private Sprite faceSprite;
        private Material faceMaterial;
        private Texture2D fallbackBackgroundTexture;
        private Sprite fallbackBackgroundSprite;

        public void UpdateRankText(int rank, string playerName, uint kills, double score, Color background, string face)
        {
            rankText.SetText(rank.ToString());
            nameText.SetText(playerName);
            killText.SetText(kills.ToString());
            scoreText.SetText(score.ToString());

            SetProfileImage(background, face);
        }

        private void SetProfileImage(Color background, string face)
        {
            if (profileImage == null)
                return;

            // profileImage is the background layer.
            EnsureBackgroundSprite();
            profileImage.color = background;

            EnsureFaceOverlay();
            ClearFace();

            if (string.IsNullOrWhiteSpace(face) || face == "null")
            {
                faceOverlay.enabled = false;
                return;
            }

            try
            {
                byte[] png = Convert.FromBase64String(face);
                faceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                {
                    name = "LeaderboardFaceTexture",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };

                if (!faceTexture.LoadImage(png, false))
                    throw new InvalidOperationException("얼굴 PNG를 읽을 수 없습니다.");

                // The saved PNG contains the opaque skin background. Make only
                // that exact color transparent so the UI background layer shows.
                RemoveBackgroundPixels(faceTexture, background);

                faceSprite = Sprite.Create(
                    faceTexture,
                    new Rect(0, 0, faceTexture.width, faceTexture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                faceSprite.name = "LeaderboardFaceSprite";

                Shader uiShader = Shader.Find("UI/Default");
                if (uiShader == null)
                    throw new InvalidOperationException("UI/Default 셰이더를 찾을 수 없습니다.");

                faceMaterial = new Material(uiShader)
                {
                    name = "LeaderboardFaceMaterial"
                };
                faceMaterial.mainTexture = faceTexture;

                faceOverlay.sprite = faceSprite;
                faceOverlay.material = faceMaterial;
                faceOverlay.color = Color.white;
                faceOverlay.enabled = true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[리더보드] 얼굴 이미지 표시 실패: {exception.Message}", this);
                ClearFace();
                faceOverlay.enabled = false;
            }
        }

        private void EnsureBackgroundSprite()
        {
            if (profileImage.sprite != null)
                return;

            fallbackBackgroundTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            fallbackBackgroundTexture.SetPixel(0, 0, Color.white);
            fallbackBackgroundTexture.Apply(false, false);
            fallbackBackgroundSprite = Sprite.Create(
                fallbackBackgroundTexture,
                new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f));
            fallbackBackgroundSprite.name = "LeaderboardBackgroundSprite";
            profileImage.sprite = fallbackBackgroundSprite;
            profileImage.type = Image.Type.Simple;
        }

        private void EnsureFaceOverlay()
        {
            if (faceOverlay != null)
                return;

            GameObject overlayObject = new GameObject("Face Overlay", typeof(RectTransform), typeof(Image));
            overlayObject.transform.SetParent(profileImage.transform, false);
            overlayObject.transform.SetAsLastSibling();

            RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            faceOverlay = overlayObject.GetComponent<Image>();
            faceOverlay.raycastTarget = false;
            faceOverlay.preserveAspect = profileImage.preserveAspect;
        }

        private static void RemoveBackgroundPixels(Texture2D texture, Color background)
        {
            Color32 target = background;
            Color32[] pixels = texture.GetPixels32();

            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].r == target.r && pixels[i].g == target.g && pixels[i].b == target.b)
                    pixels[i].a = 0;
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        private void ClearFace()
        {
            if (faceOverlay != null)
            {
                faceOverlay.sprite = null;
                faceOverlay.material = null;
            }

            if (faceSprite != null)
                Destroy(faceSprite);
            if (faceTexture != null)
                Destroy(faceTexture);
            if (faceMaterial != null)
                Destroy(faceMaterial);

            faceSprite = null;
            faceTexture = null;
            faceMaterial = null;
        }

        private void OnDestroy()
        {
            ClearFace();
            if (fallbackBackgroundSprite != null)
                Destroy(fallbackBackgroundSprite);
            if (fallbackBackgroundTexture != null)
                Destroy(fallbackBackgroundTexture);
        }
    }
}
