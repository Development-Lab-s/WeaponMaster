using DG.Tweening;
using UnityEngine;

public class BKButtonSlide : MonoBehaviour
{
    [SerializeField] private float slideDistance = 800f;
    [SerializeField] private Vector2 direction = Vector2.right;
    [SerializeField] private float slideDuration = 0.5f;
    [SerializeField] private float startDelay = 0f;
    [SerializeField] private Ease buttonEase = Ease.InCubic;
    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField] private float fadeFrom = 0f;

    
    private RectTransform rect;
    private Vector2 endPos;
    private Sequence seq;
    


    private void Awake()
    {
        rect = (RectTransform)transform;
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

        rect.anchoredPosition = endPos + direction * slideDistance; // 방향바꿀떄
        if (fadeGroup != null)
            fadeGroup.alpha = fadeFrom;

        seq = DOTween.Sequence().SetLink(gameObject);

        if (startDelay > 0f)
            seq.AppendInterval(startDelay);

        seq.Append(rect.DOAnchorPos(endPos, slideDuration).SetEase(buttonEase));

        if (fadeGroup != null)
            seq.Join(fadeGroup.DOFade(1f, slideDuration * 0.6f).SetEase(Ease.OutQuad));

        seq.OnComplete(() => rect.anchoredPosition = endPos);
        
    }
}
