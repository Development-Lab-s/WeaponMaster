using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class StablePuppetLinker
{
    private const string PuppetRootName = "Puppet";
    private const string DriverRootName = "Driver";
    private const string CharacterMeshName = "Man.010";

    [MenuItem("Tools/Ragdoll/3. Link Stable Puppet Joints")]
    private static void LinkSelectedRig()
    {
        GameObject selection = Selection.activeGameObject;
        if (selection == null)
        {
            Debug.LogError("ncharacterX2 또는 그 하위 오브젝트를 선택해야 합니다.");
            return;
        }

        if (!TryFindRigRoots(selection.transform, out Transform puppetRoot, out Transform driverRoot))
        {
            Debug.LogError(
                $"{selection.name}의 상위 계층에서 {PuppetRootName}과 {DriverRootName}를 찾지 못했습니다.",
                selection);
            return;
        }

        Dictionary<string, Transform> driverBones = BuildBoneMap(driverRoot);
        ConfigurableJoint[] puppetJoints =
            puppetRoot.GetComponentsInChildren<ConfigurableJoint>(true);

        if (puppetJoints.Length == 0)
        {
            Debug.LogError("Puppet 아래에 ConfigurableJoint가 없습니다. 조인트 변환을 먼저 실행하세요.", puppetRoot);
            return;
        }

        int linkedCount = 0;
        int skippedCount = 0;

        foreach (ConfigurableJoint joint in puppetJoints)
        {
            GameObject puppetBone = joint.gameObject;

            if (!driverBones.TryGetValue(puppetBone.name, out Transform driverBone))
            {
                Debug.LogWarning($"Driver에서 대응 뼈를 찾지 못함: {puppetBone.name}", puppetBone);
                skippedCount++;
                continue;
            }

            StablePuppetJoint stableJoint = puppetBone.GetComponent<StablePuppetJoint>();
            if (stableJoint == null)
                stableJoint = Undo.AddComponent<StablePuppetJoint>(puppetBone);

            Undo.RecordObject(stableJoint, "Link Stable Puppet Joint");

            SerializedObject serializedJoint = new SerializedObject(stableJoint);
            serializedJoint.FindProperty("animatedBone").objectReferenceValue = driverBone;
            serializedJoint.ApplyModifiedProperties();

            EditorUtility.SetDirty(stableJoint);
            PrefabUtility.RecordPrefabInstancePropertyModifications(stableJoint);
            linkedCount++;
        }

        bool pelvisLinked = LinkPelvisFollower(puppetRoot, driverBones);

        Debug.Log(
            $"StablePuppetJoint 연결 완료 — 조인트 {linkedCount}, " +
            $"골반 {(pelvisLinked ? "연결됨" : "연결 실패")}, 건너뜀 {skippedCount}",
            selection);
    }

    [MenuItem("Tools/Ragdoll/4. Add Puppet Mesh Renderer")]
    private static void AddPuppetMeshRenderer()
    {
        GameObject selection = Selection.activeGameObject;
        if (selection == null)
        {
            Debug.LogError("ncharacterX2 또는 그 하위 오브젝트를 선택해야 합니다.");
            return;
        }

        if (!TryFindRigRoots(selection.transform, out Transform puppetRoot, out Transform driverRoot))
        {
            Debug.LogError(
                $"{selection.name}의 상위 계층에서 {PuppetRootName}과 {DriverRootName}를 찾지 못했습니다.",
                selection);
            return;
        }

        SkinnedMeshRenderer driverRenderer =
            FindRenderer(driverRoot, CharacterMeshName);
        if (driverRenderer == null)
        {
            Debug.LogError($"Driver 아래에서 {CharacterMeshName} SkinnedMeshRenderer를 찾지 못했습니다.", driverRoot);
            return;
        }

        Transform existingMesh = puppetRoot.Find(CharacterMeshName);
        GameObject puppetMeshObject;
        if (existingMesh == null)
        {
            puppetMeshObject = new GameObject(CharacterMeshName);
            Undo.RegisterCreatedObjectUndo(puppetMeshObject, "Add Puppet Mesh Renderer");
            Undo.SetTransformParent(
                puppetMeshObject.transform,
                puppetRoot,
                "Parent Puppet Mesh Renderer");
        }
        else
        {
            puppetMeshObject = existingMesh.gameObject;
            Undo.RecordObject(puppetMeshObject.transform, "Update Puppet Mesh Renderer");
        }

        Transform sourceTransform = driverRenderer.transform;
        Transform targetTransform = puppetMeshObject.transform;
        targetTransform.localPosition = sourceTransform.localPosition;
        targetTransform.localRotation = sourceTransform.localRotation;
        targetTransform.localScale = sourceTransform.localScale;

        SkinnedMeshRenderer puppetRenderer =
            puppetMeshObject.GetComponent<SkinnedMeshRenderer>();
        if (puppetRenderer == null)
            puppetRenderer = Undo.AddComponent<SkinnedMeshRenderer>(puppetMeshObject);

        Undo.RecordObject(puppetRenderer, "Bind Puppet Mesh Renderer");
        EditorUtility.CopySerialized(driverRenderer, puppetRenderer);

        Dictionary<string, Transform> puppetBones = BuildBoneMap(puppetRoot);
        Transform[] reboundBones = new Transform[driverRenderer.bones.Length];

        for (int i = 0; i < driverRenderer.bones.Length; i++)
        {
            Transform driverBone = driverRenderer.bones[i];
            if (driverBone == null
                || !puppetBones.TryGetValue(driverBone.name, out reboundBones[i]))
            {
                Debug.LogError(
                    $"Puppet에서 메시 본을 찾지 못했습니다: {(driverBone == null ? "null" : driverBone.name)}",
                    puppetRoot);
                return;
            }
        }

        if (driverRenderer.rootBone == null
            || !puppetBones.TryGetValue(driverRenderer.rootBone.name, out Transform reboundRoot))
        {
            Debug.LogError("Puppet에서 SkinnedMeshRenderer Root Bone을 찾지 못했습니다.", puppetRoot);
            return;
        }

        puppetRenderer.bones = reboundBones;
        puppetRenderer.rootBone = reboundRoot;
        puppetRenderer.enabled = true;

        EditorUtility.SetDirty(puppetRenderer);
        PrefabUtility.RecordPrefabInstancePropertyModifications(targetTransform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(puppetRenderer);
        Selection.activeGameObject = puppetMeshObject;

        Debug.Log(
            $"Puppet {CharacterMeshName} 생성 및 본 {reboundBones.Length}개 재연결 완료.",
            puppetMeshObject);
    }

    private static bool TryFindRigRoots(
        Transform selection,
        out Transform puppetRoot,
        out Transform driverRoot)
    {
        for (Transform current = selection; current != null; current = current.parent)
        {
            puppetRoot = current.Find(PuppetRootName);
            driverRoot = current.Find(DriverRootName);

            if (puppetRoot != null && driverRoot != null)
                return true;
        }

        puppetRoot = null;
        driverRoot = null;
        return false;
    }

    private static Dictionary<string, Transform> BuildBoneMap(Transform root)
    {
        var bonesByName = new Dictionary<string, Transform>();

        foreach (Transform bone in root.GetComponentsInChildren<Transform>(true))
        {
            if (!bonesByName.TryAdd(bone.name, bone))
                Debug.LogWarning($"Driver에 이름이 중복된 뼈가 있습니다: {bone.name}", bone);
        }

        return bonesByName;
    }

    private static bool LinkPelvisFollower(
        Transform puppetRoot,
        IReadOnlyDictionary<string, Transform> driverBones)
    {
        Transform puppetPelvis = FindBone(puppetRoot, "pelvis");
        if (puppetPelvis == null || !driverBones.TryGetValue("pelvis", out Transform driverPelvis))
        {
            Debug.LogError("Puppet 또는 Driver에서 pelvis 뼈를 찾지 못했습니다.", puppetRoot);
            return false;
        }

        if (!puppetPelvis.TryGetComponent(out Rigidbody _))
        {
            Debug.LogError("Puppet pelvis에 Rigidbody가 없습니다.", puppetPelvis);
            return false;
        }

        PuppetPelvisFollower follower = puppetPelvis.GetComponent<PuppetPelvisFollower>();
        if (follower == null)
            follower = Undo.AddComponent<PuppetPelvisFollower>(puppetPelvis.gameObject);

        Undo.RecordObject(follower, "Link Puppet Pelvis Follower");

        SerializedObject serializedFollower = new SerializedObject(follower);
        serializedFollower.FindProperty("driverPelvis").objectReferenceValue = driverPelvis;
        serializedFollower.ApplyModifiedProperties();

        EditorUtility.SetDirty(follower);
        PrefabUtility.RecordPrefabInstancePropertyModifications(follower);
        return true;
    }

    private static Transform FindBone(Transform root, string boneName)
    {
        foreach (Transform bone in root.GetComponentsInChildren<Transform>(true))
        {
            if (bone.name == boneName)
                return bone;
        }

        return null;
    }

    private static SkinnedMeshRenderer FindRenderer(
        Transform root,
        string rendererName)
    {
        foreach (SkinnedMeshRenderer renderer in
                 root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderer.name == rendererName)
                return renderer;
        }

        return null;
    }
}
