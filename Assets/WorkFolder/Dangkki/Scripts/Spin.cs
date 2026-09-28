using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class Spin : MonoBehaviour//깡깡
{
    [Header("돌기")]
    [SerializeField] private float spinDuration = 2f;
    [SerializeField] private float spinStrength = 2f;
    [SerializeField] private float spinDistance = 5f;
    [SerializeField] private Ease spinEase = Ease.InOutCubic;
    
    [Header("좆됐을때 설정")]
    [SerializeField] private bool useLocalRotate = false;
    [SerializeField] private bool autoCenterPivot = true; 
    [SerializeField] private Vector3 pivotOffset;
    [SerializeField] private float nextDelay = 0f; 
    
    [Header("똥")]
    [SerializeField] private GameObject[] nextObjects;
    [SerializeField] private Transform modelRoot;
    private Vector3 endPos;
    private bool captured;
    private Sequence sequence;

    private void Awake()
    {
        CenterPivot();
    }
    
    private void CenterPivot()
    {
        if (modelRoot == null)
            return;

        if (autoCenterPivot)
        {
            var renderers = modelRoot.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);

                modelRoot.position += transform.position - bounds.center;
            }
        }

        modelRoot.localPosition += pivotOffset;
    }

    private void OnEnable()
    {
        transform.DOKill();

        if (!captured)
        {
            endPos = transform.position;
            captured = true;
        }

        transform.position = endPos - Vector3.up * spinDistance;

        Tween moveTween = transform.DOMove(endPos, spinDuration)
            .SetEase(Ease.OutCubic)
            .SetLink(gameObject);

        Vector3 spin = new Vector3(0, 360 * spinStrength, 0);
        Tween rotateTween = useLocalRotate
            ? transform.DOLocalRotate(spin, spinDuration, RotateMode.FastBeyond360)
            : transform.DORotate(spin, spinDuration, RotateMode.FastBeyond360);

        rotateTween.SetEase(spinEase).SetLink(gameObject);

        sequence = DOTween.Sequence()
            .Append(moveTween)
            .Join(rotateTween)
            .AppendInterval(nextDelay)
            .OnComplete(PlayNext)
            .SetLink(gameObject);
    }

    private void PlayNext()
    {
        sequence = null;

        if (nextObjects != null)
        {
            foreach (var go in nextObjects)
            {
                if (go != null)
                    go.SetActive(true);
            }
        }
        
    }

    private void OnDisable()
    {
        if (sequence != null)
        {
            sequence.Kill();
            sequence = null;
        }

        transform.DOKill();
    }
}
