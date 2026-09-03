#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class RagdollFixup
{
    static readonly string[] Bones = {
        "root","spine","chest","head",
        "upper_arm.L","forearm.L","upper_arm.R","forearm.R",
        "thigh.L","shin.L","foot.L","thigh.R","shin.R","foot.R"
    };

    [MenuItem("Tools/Ragdoll/1. Verify and Fix Feet")]
    static void Run()
    {
        var root = Selection.activeGameObject;
        if (root == null) { Debug.LogError("Visual을 선택하고 실행"); return; }

        FixFoot(root.transform, "foot.L", "shin.L");
        FixFoot(root.transform, "foot.R", "shin.R");

        int missing = 0;
        foreach (var n in Bones)
        {
            var t = RigUtil.FindDeep(root.transform, n);
            if (t == null) { Debug.LogError($"{n} — 본 자체가 없음"); missing++; continue; }

            bool rb  = t.GetComponent<Rigidbody>() != null;
            bool col = t.GetComponent<Collider>()  != null;
            bool jt  = t.GetComponent<Joint>()     != null;

            if (!rb || !col || (!jt && n != "root"))
            {
                Debug.LogWarning($"{n} — RB:{rb} Col:{col} Joint:{jt}", t);
                missing++;
            }
        }

        Debug.Log(missing == 0
            ? "검증 통과 — 14개 본 정상"
            : $"검증 실패 — {missing}개 문제. 위 경고 확인");
    }

    static void FixFoot(Transform root, string footName, string shinName)
    {
        var foot = RigUtil.FindDeep(root, footName);
        var shin = RigUtil.FindDeep(root, shinName);
        if (foot == null || shin == null) return;

        bool changed = false;

        if (foot.GetComponent<Collider>() == null)
        {
            var box = Undo.AddComponent<BoxCollider>(foot.gameObject);
            box.size   = new Vector3(0.09f, 0.05f, 0.22f);
            box.center = new Vector3(0f, 0f, 0.06f);
            changed = true;
        }

        var rb = foot.GetComponent<Rigidbody>();
        if (rb == null) { rb = Undo.AddComponent<Rigidbody>(foot.gameObject); changed = true; }
        rb.mass = 1.2f;

        if (foot.GetComponent<Joint>() == null)
        {
            var shinRb = shin.GetComponent<Rigidbody>();
            if (shinRb == null) { Debug.LogError($"{shinName}에 Rigidbody 없음"); return; }

            var cj = Undo.AddComponent<CharacterJoint>(foot.gameObject);
            cj.connectedBody  = shinRb;
            cj.lowTwistLimit  = new SoftJointLimit { limit = -30f };
            cj.highTwistLimit = new SoftJointLimit { limit =  30f };
            cj.swing1Limit    = new SoftJointLimit { limit =  15f };
            cj.swing2Limit    = new SoftJointLimit { limit =  10f };
            changed = true;
        }

        if (changed) Debug.Log($"{footName} 보충 완료 — Box Collider 크기는 씬뷰에서 확인", foot);
    }
}
#endif