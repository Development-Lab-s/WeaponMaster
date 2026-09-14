using DG.Tweening;
using UnityEngine;


public class TextDropIn : MonoBehaviour
{
    [SerializeField] private float startScaleMultiplier = 6f;
    [SerializeField] private float dropDuration = 0.35f;
    [SerializeField] private float startDelay = 0f;
    [SerializeField] private Ease dropEase = Ease.InCubic;
    [SerializeField] private float punchScale = 0.2f;
    [SerializeField] private float shakeStrength = 15f;
    [SerializeField] private float impactDuration = 0.25f;
    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField] private float fadeFrom = 0f;

    
    
    
    private RectTransform rect;
    private Vector3 endScale;
    private Vector2 endPos;
    private Sequence seq;

    private void Awake()
    {
        rect = (RectTransform)transform;
        endScale = rect.localScale;
        endPos = rect.anchoredPosition;
    }

    private void OnEnable()
    {
        Play();
    }

    private void OnDisable()
    {
        rect.DOKill();
        if (fadeGroup != null)
            fadeGroup.DOKill();
    }

    public void Play()
    {
        rect.DOKill();
        if (fadeGroup != null)
            fadeGroup.DOKill();
        
        rect.localScale = endScale * startScaleMultiplier;
        rect.anchoredPosition = endPos;
        if (fadeGroup != null)
            fadeGroup.alpha = fadeFrom;

        seq = DOTween.Sequence().SetLink(gameObject);

        if (startDelay > 0f)
            seq.AppendInterval(startDelay);

        seq.Append(rect.DOScale(endScale, dropDuration).SetEase(dropEase));

        if (fadeGroup != null)
            seq.Join(fadeGroup.DOFade(1f, dropDuration * 0.6f).SetEase(Ease.OutQuad));
        
       seq.Append(rect.DOPunchScale(endScale * punchScale, impactDuration, 8, 1f));
       if (shakeStrength > 0f)
            seq.Join(rect.DOShakeAnchorPos(impactDuration, shakeStrength, 20, 90f, false, true));

        seq.OnComplete(() =>
        {
            rect.localScale = endScale;
            rect.anchoredPosition = endPos;
        });
        
    }
}
