using DG.Tweening;
using UnityEngine;

public class Spin : MonoBehaviour
{
    [SerializeField] private float spinDuration = 2f;
    [SerializeField] private float spinDistance = 5f;
    [SerializeField] private bool useLocalRotate = false;
    
    [SerializeField] private Transform modelRoot;
    [SerializeField] private bool autoCenterPivot = true; 
    [SerializeField] private Vector3 pivotOffset; 

    private Vector3 endPos;
    private bool captured;

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

        transform.DOMove(endPos, spinDuration)
            .SetEase(Ease.OutCubic)
            .SetLink(gameObject);

        Vector3 spin = new Vector3(0, 360, 0);
        Tween rotateTween = useLocalRotate
            ? transform.DOLocalRotate(spin, spinDuration, RotateMode.FastBeyond360)
            : transform.DORotate(spin, spinDuration, RotateMode.FastBeyond360);

        rotateTween.SetEase(Ease.OutCubic).SetLink(gameObject);
    }

    private void OnDisable()
    {
        transform.DOKill();
    }
}
