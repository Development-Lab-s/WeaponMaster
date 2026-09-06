using UnityEngine;

public class JointDebug : MonoBehaviour
{
    ConfigurableJoint joint;
    PuppetJoint puppet;

    void Start()
    {
        joint = GetComponent<ConfigurableJoint>();
        puppet = GetComponent<PuppetJoint>();

        var rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (Time.frameCount % 60 != 0) return;
    }
}