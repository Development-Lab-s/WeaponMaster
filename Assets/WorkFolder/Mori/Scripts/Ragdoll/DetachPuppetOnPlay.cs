using UnityEngine;

[DefaultExecutionOrder(-10000)]
[DisallowMultipleComponent]
public sealed class DetachPuppetOnPlay : MonoBehaviour
{
    private void Awake()
    {
        if (transform.parent == null)
            return;

        transform.SetParent(null, true);
    }
}
