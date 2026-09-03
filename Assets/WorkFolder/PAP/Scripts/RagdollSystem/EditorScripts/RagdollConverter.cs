#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class RagdollConverter
{
    [MenuItem("Tools/Ragdoll/2. Convert to ConfigurableJoint")]
    static void Convert()
    {
        var root = Selection.activeGameObject;
        if (root == null) { Debug.LogError("Visual을 선택하고 실행"); return; }

        var joints = root.GetComponentsInChildren<CharacterJoint>(true);
        if (joints.Length == 0)
        {
            Debug.LogWarning("CharacterJoint가 없음. 이미 변환했거나 위저드를 안 돌림");
            return;
        }

        int n = 0;
        foreach (var cj in joints)
        {
            var go = cj.gameObject;

            var body   = cj.connectedBody;
            var anchor = cj.anchor;
            var axis   = cj.axis;
            var swing  = cj.swingAxis;
            float low  = cj.lowTwistLimit.limit;
            float high = cj.highTwistLimit.limit;
            float sw1  = cj.swing1Limit.limit;
            float sw2  = cj.swing2Limit.limit;

            Undo.DestroyObjectImmediate(cj);

            var j = Undo.AddComponent<ConfigurableJoint>(go);
            j.connectedBody = body;
            j.anchor        = anchor;
            j.axis          = axis;
            j.secondaryAxis = swing;

            j.xMotion = j.yMotion = j.zMotion = ConfigurableJointMotion.Locked;
            j.angularXMotion = j.angularYMotion = j.angularZMotion
                = ConfigurableJointMotion.Limited;

            j.lowAngularXLimit  = new SoftJointLimit { limit = low };
            j.highAngularXLimit = new SoftJointLimit { limit = high };
            j.angularYLimit     = new SoftJointLimit { limit = sw1 };
            j.angularZLimit     = new SoftJointLimit { limit = sw2 };

            j.rotationDriveMode   = RotationDriveMode.Slerp;
            j.projectionMode      = JointProjectionMode.PositionAndRotation;
            j.projectionDistance  = 0.05f;
            j.projectionAngle     = 10f;
            j.enablePreprocessing = false;

            // 드라이브는 PuppetJoint가 런타임에 질량 기반으로 계산
            j.slerpDrive = new JointDrive {
                positionSpring = 0f,
                positionDamper = 0f,
                maximumForce   = Mathf.Infinity
            };

            var rb = go.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.solverIterations         = 16;
                rb.solverVelocityIterations = 8;
                rb.maxAngularVelocity       = 20f;
                rb.interpolation            = RigidbodyInterpolation.None;
            }
            n++;
        }

        // 골반 고정
        var hips = RigUtil.FindDeep(root.transform, "root") ?? root.transform;
        var hipRb = hips.GetComponent<Rigidbody>();
        if (hipRb != null)
        {
            hipRb.isKinematic  = true;
            hipRb.interpolation = RigidbodyInterpolation.Interpolate;
            Debug.Log($"골반({hips.name}) Kinematic 설정", hips);
        }

        Debug.Log($"변환 완료 — {n}개 조인트");
    }
}
#endif