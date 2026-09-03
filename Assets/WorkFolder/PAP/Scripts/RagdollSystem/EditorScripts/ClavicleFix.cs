#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ClavicleFix
{
    [MenuItem("Tools/Ragdoll/4. Insert Clavicles")]
    static void Run()
    {
        var root = Selection.activeGameObject;
        if (root == null) return;

        Insert(root.transform, "clavicle.L", "upper_arm.L");
        Insert(root.transform, "clavicle.R", "upper_arm.R");
        Debug.Log("쇄골 삽입 완료");
    }

    static void Insert(Transform root, string clavName, string armName)
    {
        var clav = RigUtil.FindDeep(root, clavName);
        var arm  = RigUtil.FindDeep(root, armName);
        if (clav == null || arm == null) { Debug.LogError($"{clavName} 또는 {armName} 없음"); return; }

        var armJoint = arm.GetComponent<ConfigurableJoint>();
        if (armJoint == null) { Debug.LogError($"{armName}에 조인트 없음"); return; }

        // 팔이 원래 붙어있던 부모 (spine 또는 chest)
        var parentBody = armJoint.connectedBody;
        if (parentBody == null) { Debug.LogError($"{armName}의 Connected Body 비어있음"); return; }

        // 쇄골에 물리 추가
        if (clav.GetComponent<Collider>() == null)
        {
            var cap = Undo.AddComponent<CapsuleCollider>(clav.gameObject);
            cap.radius = 0.05f;
            cap.height = 0.15f;
            cap.direction = 0;
        }

        var rb = clav.GetComponent<Rigidbody>();
        if (rb == null) rb = Undo.AddComponent<Rigidbody>(clav.gameObject);
        rb.mass = 1.5f;
        rb.solverIterations = 16;
        rb.interpolation = RigidbodyInterpolation.None;

        var cj = clav.GetComponent<ConfigurableJoint>();
        if (cj == null) cj = Undo.AddComponent<ConfigurableJoint>(clav.gameObject);

        cj.connectedBody = parentBody;          // 쇄골 → 몸통
        cj.xMotion = cj.yMotion = cj.zMotion = ConfigurableJointMotion.Locked;
        cj.angularXMotion = cj.angularYMotion = cj.angularZMotion
            = ConfigurableJointMotion.Limited;
        cj.lowAngularXLimit  = new SoftJointLimit { limit = -15f };
        cj.highAngularXLimit = new SoftJointLimit { limit =  15f };
        cj.angularYLimit     = new SoftJointLimit { limit =  20f };
        cj.angularZLimit     = new SoftJointLimit { limit =  15f };
        cj.rotationDriveMode = RotationDriveMode.Slerp;
        cj.projectionMode = JointProjectionMode.PositionAndRotation;
        cj.projectionDistance = 0.05f;
        cj.projectionAngle = 10f;
        cj.enablePreprocessing = false;

        // 팔을 쇄골에 재연결
        armJoint.connectedBody = rb;

        Debug.Log($"{clavName} 삽입 — {armName}의 부모를 {parentBody.name} → {clavName}로 변경", clav);
    }
}
#endif