using DG.Tweening;
using UnityEngine;

public class BKSlideText : MonoBehaviour
{
    [SerializeField] private float slideDistance = 500f;
    [SerializeField] private float slideDuration = 0.5f;
    [SerializeField] private float startDelay = 0f;
    [SerializeField] private Ease slideEase = Ease.OutCubic;
    [SerializeField] private CanvasGroup fadeGroup;

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
        seq?.Kill();
        seq = null;
    }

    public void Play()
    {
        seq?.Kill();

        // 원래 위치보다 왼쪽에서 시작
        rect.anchoredPosition =
            endPos + Vector2.left * slideDistance;

        if (fadeGroup != null)
            fadeGroup.alpha = 0f;

        seq = DOTween.Sequence().SetLink(gameObject);

        if (startDelay > 0f)
            seq.AppendInterval(startDelay);

        // 오른쪽으로 이동해서 원래 위치에 도착
        seq.Append(
            rect.DOAnchorPos(endPos, slideDuration)
                .SetEase(slideEase)
        );

        // 이동하면서 서서히 표시
        if (fadeGroup != null)
        {
            seq.Join(
                fadeGroup.DOFade(1f, slideDuration)
                    .SetEase(Ease.OutQuad)
            );
        }
    }
}