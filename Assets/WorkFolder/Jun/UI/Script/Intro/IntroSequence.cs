using DG.Tweening;
using UnityEngine;

public class IntroSequence : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform character;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform explosionPoint;
    [SerializeField] private GameObject explosionPrefab;

    [Header("UI References")]
    [SerializeField] private RectTransform leftTitle;
    [SerializeField] private RectTransform rightTitle;
    [SerializeField] private GameObject buttonsRoot;

    [Header("Timing")]
    [SerializeField] private float startDelay = 2.2f;
    [SerializeField] private float riseDuration = 1.8f;
    [SerializeField] private float settleDuration = 0.2f;
    [SerializeField] private float uiDelay = 0.1f;
    [SerializeField] private float titleDuration = 0.35f;

    [Header("Character")]
    [SerializeField] private float riseDistance = 12f;
    [SerializeField] private float overshoot = 0.4f;
    [SerializeField] private float rotationDegrees = 360f;

    [Header("Camera Shake")]
    [SerializeField] private float shakeDuration = 0.15f;
    [SerializeField] private float shakeStrength = 0.15f;

    private Sequence sequence;
    private Vector2 leftTitlePosition;
    private Vector2 rightTitlePosition;

    private void Start()
    {
        leftTitlePosition = leftTitle.anchoredPosition;
        rightTitlePosition = rightTitle.anchoredPosition;
        leftTitle.gameObject.SetActive(false);
        rightTitle.gameObject.SetActive(false);
        buttonsRoot.SetActive(false);

        Vector3 endPosition = character.position;
        Vector3 endRotation = character.eulerAngles;
        character.position = endPosition - Vector3.up * riseDistance;

        sequence = DOTween.Sequence().SetDelay(startDelay);
        sequence.Append(character.DOMoveY(endPosition.y + overshoot, riseDuration).SetEase(Ease.OutCubic));
        sequence.Join(character.DORotate(endRotation + Vector3.up * rotationDegrees, riseDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.OutCubic));
        sequence.Append(character.DOMoveY(endPosition.y, settleDuration).SetEase(Ease.OutQuad));
        sequence.AppendCallback(PlayExplosion);
        sequence.AppendInterval(uiDelay);
        sequence.AppendCallback(ShowUI);
    }

    private void PlayExplosion()
    {
        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(explosionPrefab, explosionPoint.position, explosionPoint.rotation);
            Destroy(explosion, 10f);
        }

        targetCamera.transform.DOShakePosition(shakeDuration, shakeStrength);
    }

    private void ShowUI()
    {
        Vector2 center = (leftTitlePosition + rightTitlePosition) * 0.5f;

        leftTitle.gameObject.SetActive(true);
        rightTitle.gameObject.SetActive(true);
        leftTitle.anchoredPosition = center;
        rightTitle.anchoredPosition = center;
        leftTitle.DOAnchorPos(leftTitlePosition, titleDuration).SetEase(Ease.OutCubic);
        rightTitle.DOAnchorPos(rightTitlePosition, titleDuration).SetEase(Ease.OutCubic);

        buttonsRoot.SetActive(true);
    }

    private void OnDestroy()
    {
        sequence?.Kill();
    }
}
