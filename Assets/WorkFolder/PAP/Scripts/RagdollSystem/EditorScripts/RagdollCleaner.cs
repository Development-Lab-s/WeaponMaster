#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class RagdollCleaner
{
    [MenuItem("Tools/Ragdoll/0. Clean Selected")]
    static void Clean()
    {
        var root = Selection.activeGameObject;
        if (root == null) { Debug.LogError("오브젝트를 선택하고 실행"); return; }

        if (!EditorUtility.DisplayDialog("Clean Ragdoll",
                $"{root.name} 아래의 모든 Joint / Rigidbody / Collider / PuppetJoint를 제거합니다.",
                "실행", "취소")) return;

        int n = 0;
        foreach (var c in root.GetComponentsInChildren<PuppetJoint>(true)) { Object.DestroyImmediate(c); n++; }
        foreach (var c in root.GetComponentsInChildren<Joint>(true))       { Object.DestroyImmediate(c); n++; }
        foreach (var c in root.GetComponentsInChildren<Rigidbody>(true))   { Object.DestroyImmediate(c); n++; }
        foreach (var c in root.GetComponentsInChildren<Collider>(true))    { Object.DestroyImmediate(c); n++; }

        Debug.Log($"{root.name} 정리 완료 — {n}개 제거");
    }
}
#endif