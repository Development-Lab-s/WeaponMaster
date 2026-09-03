using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PuppetLinker : MonoBehaviour
{
    [SerializeField] Transform skeletonRoot;
    [SerializeField] Transform visualRoot;

    [SerializeField] string[] drivenBones = {
        "spine", "chest", "head",
        "upper_arm.L", "forearm.L",
        "upper_arm.R", "forearm.R"
    };

#if UNITY_EDITOR
    [ContextMenu("Link Puppet Joints")]
    void Link()
    {
        if (skeletonRoot == null || visualRoot == null)
        {
            Debug.LogError("skeletonRoot / visualRoot를 먼저 연결");
            return;
        }

        int ok = 0;
        foreach (var name in drivenBones)
        {
            var vis  = RigUtil.FindDeep(visualRoot, name);
            var skel = RigUtil.FindDeep(skeletonRoot, name);

            if (vis == null)  { Debug.LogError($"Visual에 {name} 없음"); continue; }
            if (skel == null) { Debug.LogError($"Skeleton에 {name} 없음"); continue; }

            if (vis.GetComponent<ConfigurableJoint>() == null)
            {
                Debug.LogError($"{name}에 ConfigurableJoint 없음 — 2번 스크립트 먼저 실행", vis);
                continue;
            }

            var pj = vis.GetComponent<PuppetJoint>();
            if (pj == null) pj = Undo.AddComponent<PuppetJoint>(vis.gameObject);

            var so = new SerializedObject(pj);
            so.FindProperty("animatedBone").objectReferenceValue = skel;
            so.ApplyModifiedProperties();

            ok++;
        }

        Debug.Log($"PuppetJoint 연결: {ok}/{drivenBones.Length}");
    }

    [ContextMenu("Validate Rig Match")]
    void Validate()
    {
        var s = skeletonRoot.GetComponentsInChildren<Transform>();
        var v = visualRoot.GetComponentsInChildren<Transform>();

        if (s.Length != v.Length)
            Debug.LogWarning($"본 개수 불일치 — Skeleton {s.Length} / Visual {v.Length}");

        int bad = 0;
        for (int i = 0; i < Mathf.Min(s.Length, v.Length); i++)
        {
            if (s[i].name != v[i].name)
            {
                Debug.LogError($"[{i}] 이름 불일치 — {s[i].name} vs {v[i].name}");
                bad++;
                continue;
            }

            float diff = Quaternion.Angle(s[i].localRotation, v[i].localRotation);
            if (diff > 1f)
            {
                Debug.LogWarning($"{s[i].name} 초기 회전 차이 {diff:F1}도", v[i]);
                bad++;
            }
        }

        Debug.Log(bad == 0 ? "리그 일치 확인" : $"{bad}개 문제 발견");
    }
#endif
}