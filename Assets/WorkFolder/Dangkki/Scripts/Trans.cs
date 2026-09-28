using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class Trans : MonoBehaviour
{

    public static Trans Instance { get; private set; }
    [Header("선")]
    [SerializeField] private float lineAngle = 30f;
    [SerializeField] private float lineThickness = 60f;
    [SerializeField] private Color lineColor = Color.white;

    [Header("타이밍")]
    [SerializeField] private float coverDuration = 0.6f;
    [SerializeField] private Ease coverEase = Ease.InOutCubic;
    [SerializeField] private float revealDuration = 0.6f;
    [SerializeField] private Ease revealEase = Ease.InOutCubic;
    
    [Header("이벤트")]
    public UnityEvent onCovered;  // 덮힘
    public UnityEvent onFinished; // 끝났을떄
    
    
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image blocker;
    [SerializeField] private RectTransform wiper;
    [SerializeField] private RectTransform cover;
    [SerializeField] private RectTransform background;
    [SerializeField] private Image line;

    public bool IsPlaying { get; private set; }

    private RectTransform root;
    private Sequence seq;
    private Vector2 dir;
    private float travel;

    private void Awake()
    {
        root = (RectTransform)transform;
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        SetIdle();
    }

    private void OnDestroy()
    {
        Kill();
        if (Instance == this) Instance = null;
    }
    
    public void Play(Action onCoveredAction = null)
    {
        Begin();

        seq = DOTween.Sequence().SetLink(gameObject);
        seq.Append(BuildCoverTween());
        seq.AppendCallback(() =>
        {
            onCoveredAction?.Invoke();
            onCovered?.Invoke();
        });
    }

    public void DePlay(Action onFinishedAction = null)
    {
        seq = DOTween.Sequence().SetLink(gameObject);
        seq.Append(BuildRevealTween());
        seq.OnComplete(() =>
        {
            SetIdle();
            onFinishedAction?.Invoke();
            onFinished?.Invoke();
        });
    }
    
    public void Cover(Action onCoveredAction = null)
    {
        Begin();

        seq = DOTween.Sequence().SetLink(gameObject);
        seq.Append(BuildCoverTween());
        seq.OnComplete(() =>
        {
            onCoveredAction?.Invoke();
            onCovered?.Invoke();
        });
    }
    
    public void Reveal(Action onFinishedAction = null)
    {
        Begin();

        seq = DOTween.Sequence().SetLink(gameObject);
        seq.Append(BuildRevealTween());
        seq.OnComplete(() =>
        {
            SetIdle();
            onFinishedAction?.Invoke();
            onFinished?.Invoke();
        });
    }

    public void Kill()
    {
        seq?.Kill();
        seq = null;
    }

    private void Begin()//많이쓰는거
    {
        Kill();
        IsPlaying = true;
        blocker.enabled = true;
        
        //갈아엎기
        Vector2 size = root.rect.size;
        float rad = lineAngle * Mathf.Deg2Rad;

        dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        travel = size.x * 0.5f * Mathf.Abs(dir.x) + size.y * 0.5f * Mathf.Abs(dir.y) + lineThickness * 0.5f + 10f;

        float big = 2f * (size.magnitude + lineThickness);

        wiper.localEulerAngles = new Vector3(0f, 0f, lineAngle);
        wiper.sizeDelta = new Vector2(big, big);

        line.rectTransform.sizeDelta = new Vector2(lineThickness, big);
        line.color = lineColor;

        background.sizeDelta = size;
        background.localEulerAngles = new Vector3(0f, 0f, -lineAngle);

        SyncBackground();
    }




    private Tween BuildCoverTween() // 반대로 하려면 mask 어쩌고는 0으로 reverse는 1
    {
        float sign = -1f;
        Vector2 from = dir * travel * sign;
        Vector2 to = -from;
        float maskSide = 1f;
        return MoveTween(from, to, coverDuration, coverEase, maskSide);
    }

    private Tween BuildRevealTween()
    {
        float sign = -1f;
        Vector2 coverStart = dir * travel * sign;
        Vector2 coverEnd = -coverStart;
        Vector2 from, to;
        float maskSide;
        from = coverStart; to = coverEnd;
        maskSide = 0f;

        //from = coverEnd; to = coverStart;
        //maskSide = 1f;
        // 왔다가기(안쓸듯)
        return MoveTween(from, to, revealDuration, revealEase, maskSide);
    }

    private Tween MoveTween(Vector2 from, Vector2 to, float duration, Ease ease, float maskSide)
    {
        return DOTween.To(() => 0f, t =>
            {
                wiper.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
                SyncBackground();
            }, 1f, duration)
            .SetEase(ease)
            .OnStart(() =>
            {
                wiper.pivot = new Vector2(maskSide, 0.5f);
                RectTransform lt = line.rectTransform;
                lt.anchorMin = lt.anchorMax = new Vector2(maskSide, 0.5f);
                lt.anchoredPosition = Vector2.zero;
                wiper.anchoredPosition = from;
                SyncBackground();
            });
    }


    private void SyncBackground() // 배경 고정 (없으면 좆댐)
    {
        background.position = root.TransformPoint(root.rect.center);
        background.rotation = root.rotation;
    }

    private void SetIdle()
    {
        IsPlaying = false;
        blocker.enabled = false;
        wiper.anchoredPosition = new Vector2(99999f, 0f); // 내보내기(문제 될수도 있음)
    }
}
