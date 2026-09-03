using System.Collections.Generic;
using UnityEngine;

public class BoneCopier : MonoBehaviour
{
    [SerializeField] Transform skeletonRoot;
    [SerializeField] Transform visualRoot;

    [SerializeField] string[] copiedBones = {
        "thigh.L", "shin.L", "foot.L",
        "thigh.R", "shin.R", "foot.R"
    };

    Transform[] src, dst;

    void Awake()
    {
        var a = new List<Transform>();
        var b = new List<Transform>();

        foreach (var n in copiedBones)
        {
            var s = RigUtil.FindDeep(skeletonRoot, n);
            var v = RigUtil.FindDeep(visualRoot, n);
            if (s != null && v != null) { a.Add(s); b.Add(v); }
            else Debug.LogWarning($"BoneCopier: {n} 매칭 실패");
        }

        src = a.ToArray();
        dst = b.ToArray();
    }

    void LateUpdate()
    {
        for (int i = 0; i < src.Length; i++)
            dst[i].localRotation = src[i].localRotation;
    }
}