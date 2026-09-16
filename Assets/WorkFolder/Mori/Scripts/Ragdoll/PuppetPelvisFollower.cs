using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class PuppetPelvisFollower : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform driverPelvis;

    [Header("Follow")]
    [SerializeField] private bool followRotation = true;

    private Rigidbody _pelvisBody;

    public Transform DriverPelvis => driverPelvis;

    private void Awake()
    {
        _pelvisBody = GetComponent<Rigidbody>();

        Transform puppetRoot = transform.parent.parent;
        puppetRoot.SetParent(null, true);
    }

    private void OnEnable()
    {
        if (_pelvisBody == null)
            _pelvisBody = GetComponent<Rigidbody>();

        if (driverPelvis == null)
        {
            Debug.LogError("Driver Pelvis가 할당되지 않았습니다.", this);
            enabled = false;
            return;
        }

        _pelvisBody.isKinematic = true;
        _pelvisBody.interpolation = RigidbodyInterpolation.Interpolate;
        SnapToDriver();
    }

    public void MoveWithDriverRoot(
        Transform driverRoot,
        Vector3 nextRootPosition,
        Quaternion nextRootRotation)
    {
        Vector3 pelvisPositionInRoot =
            driverRoot.InverseTransformPoint(driverPelvis.position);
        Quaternion pelvisRotationInRoot =
            Quaternion.Inverse(driverRoot.rotation) * driverPelvis.rotation;

        Vector3 nextPelvisPosition = nextRootPosition
            + nextRootRotation
            * Vector3.Scale(pelvisPositionInRoot, driverRoot.lossyScale);
        Quaternion nextPelvisRotation =
            nextRootRotation * pelvisRotationInRoot;

        _pelvisBody.MovePosition(nextPelvisPosition);

        if (followRotation)
            _pelvisBody.MoveRotation(nextPelvisRotation);
    }

    private void OnDisable()
    {
        if (_pelvisBody != null && Application.isPlaying)
            _pelvisBody.isKinematic = false;
    }

    private void SnapToDriver()
    {
        _pelvisBody.position = driverPelvis.position;

        if (followRotation)
            _pelvisBody.rotation = driverPelvis.rotation;
    }
}
