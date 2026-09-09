using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class Title : MonoBehaviour
{
    [SerializeField] private float titleDistance = 300f;
    [SerializeField] private float showDuration = 1f;
    [SerializeField] private float startDelay = 0.5f;

    private RectTransform rect;
    private Vector3 endPos;
    private Vector3 endScale;

    private void Awake()
    {
        rect = (RectTransform)transform;
        endPos = rect.anchoredPosition3D;
    }

    private void OnEnable()
    {
        rect.anchoredPosition3D = endPos + Vector3.back * titleDistance;

        rect.DOAnchorPos3D(endPos, showDuration)
            .SetDelay(startDelay)
            //.SetEase(Ease.OutCubic)
            .SetLink(gameObject);

        rect.DOScale(endScale, showDuration)
            .SetDelay(startDelay)
            //.SetEase(Ease.OutCubic)
            .SetLink(gameObject);
    }

    private void OnDisable()
    {
        rect.DOKill();
    }
}
