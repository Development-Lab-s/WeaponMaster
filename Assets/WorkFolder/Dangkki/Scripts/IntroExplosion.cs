using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Video;
using DG.Tweening;

public class IntroExplosion : MonoBehaviour
{
    [Serializable]
    public class TimedUI
    {
        public GameObject target;
        public float delaySeconds = 2f;
    }

    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private Renderer screen;                       // 월드에 놓는 Quad
    [SerializeField] private string materialProperty = "_BaseMap";  // URP Unlit 기준
    [SerializeField] private bool billboard = true;
    [SerializeField] private bool muteVideo = true;      // 오디오 트랙 디코딩 생략
    [SerializeField] private bool dropFrames = false;    // false면 프레임 안 버림
    [SerializeField] private int warmupFrames = 2;       // 준비 후 몇 프레임 쉬었다 재생
    [SerializeField] private float punchScale = 0.15f;              // 터질 때 살짝 부풀리기

    [Header("Impact")]
    [SerializeField] private Light flashLight;
    [SerializeField] private float flashIntensity = 30f;
    [SerializeField] private float flashDuration = 0.35f;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float shakeStrength = 0.6f;
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float fovKick = 8f;
    [SerializeField] private CanvasGroup whiteFlash;                // 화면 전체 흰색 Image (선택)

    [Header("UI")]
    [SerializeField] private TimedUI[] uiToShow;

    private Camera cam;
    private Vector3 screenBaseScale;
    private float baseFov;
    private Coroutine showCo;

    private void Awake()
    {
        cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam != null)
            baseFov = cam.fieldOfView;

        if (screen != null)
            screenBaseScale = screen.transform.localScale;

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.MaterialOverride;
        videoPlayer.skipOnDrop = dropFrames;
        videoPlayer.waitForFirstFrame = true;
        if (muteVideo)
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        videoPlayer.targetMaterialRenderer = screen;
        videoPlayer.targetMaterialProperty = materialProperty;

        videoPlayer.prepareCompleted += OnPrepared;
        videoPlayer.loopPointReached += OnVideoFinished;

        if (screen != null)
            screen.gameObject.SetActive(false);
        if (flashLight != null)
            flashLight.intensity = 0f;
        if (whiteFlash != null)
            whiteFlash.alpha = 0f;

        HideAll();
    }

    private void OnDestroy()
    {
        videoPlayer.prepareCompleted -= OnPrepared;
        videoPlayer.loopPointReached -= OnVideoFinished;
    }

    private void Start()
    {
        Play();
    }

    private void LateUpdate()
    {
        if (!billboard || screen == null || cam == null)
            return;

        // 어느 각도에서 봐도 정면으로 보이게
        screen.transform.rotation = cam.transform.rotation;
    }

    public void Play()
    {
        if (showCo != null)
            StopCoroutine(showCo);

        HideAll();
        videoPlayer.Prepare();
    }

    private void HideAll()
    {
        foreach (var ui in uiToShow)
        {
            if (ui.target != null)
                ui.target.SetActive(false);
        }
    }

    private void OnPrepared(VideoPlayer vp)
    {
        StartCoroutine(WarmupRoutine(vp));
    }

    // 준비 직후 바로 틀면 씬 로드 스파이크와 겹쳐서 첫 프레임이 튄다
    private IEnumerator WarmupRoutine(VideoPlayer vp)
    {
        for (int i = 0; i < warmupFrames; i++)
            yield return null;

        if (screen != null)
        {
            screen.gameObject.SetActive(true);
            screen.transform.localScale = screenBaseScale;
            screen.transform.DOPunchScale(screenBaseScale * punchScale, 0.35f, 6, 1f)
                            .SetLink(screen.gameObject);
        }

        vp.Play();
        Impact();

        showCo = StartCoroutine(ShowRoutine());
    }

    private void Impact()
    {
        if (flashLight != null)
        {
            flashLight.intensity = flashIntensity;
            DOTween.To(() => flashLight.intensity, v => flashLight.intensity = v, 0f, flashDuration)
                   .SetEase(Ease.OutQuad)
                   .SetLink(gameObject);
        }

        if (whiteFlash != null)
        {
            whiteFlash.DOKill();
            whiteFlash.alpha = 1f;
            whiteFlash.DOFade(0f, flashDuration * 1.5f).SetEase(Ease.OutQuad).SetLink(gameObject);
        }

        if (cam != null)
        {
            if (shakeStrength > 0f)
                cam.transform.DOShakePosition(shakeDuration, shakeStrength, 20, 90f, false, true)
                             .SetLink(cam.gameObject);

            if (fovKick > 0f)
            {
                cam.fieldOfView = baseFov - fovKick;
                cam.DOFieldOfView(baseFov, shakeDuration).SetEase(Ease.OutCubic).SetLink(cam.gameObject);
            }
        }
    }

    private IEnumerator ShowRoutine()
    {
        var ordered = uiToShow.Where(ui => ui.target != null)
                              .OrderBy(ui => ui.delaySeconds)
                              .ToArray();

        float elapsed = 0f;
        foreach (var ui in ordered)
        {
            float wait = ui.delaySeconds - elapsed;
            if (wait > 0f)
            {
                yield return new WaitForSeconds(wait);
                elapsed += wait;
            }

            ui.target.SetActive(true);
        }

        showCo = null;
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        if (screen != null)
            screen.gameObject.SetActive(false);
    }
}
