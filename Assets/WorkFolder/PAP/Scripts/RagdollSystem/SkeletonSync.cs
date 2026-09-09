using Fusion;
using UnityEngine;

namespace WorkFolder.PAP.Scripts.RagdollSystem
{
    public class SkeletonSync : NetworkBehaviour
    {
        [SerializeField] Transform[] bones;

        [Networked, Capacity(18)]
        NetworkArray<Quaternion> BoneRotations { get; }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;
            for (int i = 0; i < bones.Length; i++)
                BoneRotations.Set(i, bones[i].localRotation);
        }

        public override void Render()
        {
            if (HasStateAuthority) return;
            for (int i = 0; i < bones.Length; i++)
                bones[i].localRotation = BoneRotations[i];
        }
    }
}