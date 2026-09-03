using UnityEngine;

public class JointDebug : MonoBehaviour
{
    ConfigurableJoint joint;
    PuppetJoint puppet;

    void Start()
    {
        joint = GetComponent<ConfigurableJoint>();
        puppet = GetComponent<PuppetJoint>();

        Debug.Log($"=== {name} ===");
        Debug.Log($"ConnectedBody: {(joint.connectedBody ? joint.connectedBody.name : "NULL")}");
        Debug.Log($"Motion: {joint.xMotion}/{joint.yMotion}/{joint.zMotion}");
        Debug.Log($"Angular: {joint.angularXMotion}/{joint.angularYMotion}/{joint.angularZMotion}");
        Debug.Log($"DriveMode: {joint.rotationDriveMode}");
        Debug.Log($"Spring: {joint.slerpDrive.positionSpring}  Damper: {joint.slerpDrive.positionDamper}  MaxForce: {joint.slerpDrive.maximumForce}");

        var rb = GetComponent<Rigidbody>();
        Debug.Log($"Mass: {rb.mass}  Kinematic: {rb.isKinematic}");
        Debug.Log($"PuppetJoint enabled: {(puppet ? puppet.enabled.ToString() : "NULL")}");
    }

    void FixedUpdate()
    {
        if (Time.frameCount % 60 != 0) return;
        Debug.Log($"target: {joint.targetRotation.eulerAngles}  현재로컬: {transform.localRotation.eulerAngles}");
    }
}